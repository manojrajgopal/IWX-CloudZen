using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Renci.SshNet;
using Renci.SshNet.Common;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.EC2Connection.DTOs;
using IWX_CloudZen.CloudServices.EC2Connection.Models;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Factory;
using IWX_CloudZen.CloudServices.SecurityGroups.DTOs;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.EC2Connection.Services
{
    public class Ec2ConnectionService : IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Ec2ConnectionService> _logger;

        private static readonly ConcurrentDictionary<string, SshSession> _sessions = new();

        public Ec2ConnectionService(
            IServiceScopeFactory scopeFactory,
            ILogger<Ec2ConnectionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ====================================================================
        // CONNECT — supports both SSM (default) and SSH
        // ====================================================================

        public async Task<StartConnectionResponse> Connect(
            string user, int accountId, StartConnectionRequest request)
        {
            var method = (request.ConnectionMethod ?? "SSM").Trim().ToUpperInvariant();
            if (method != "SSM" && method != "SSH")
                throw new InvalidOperationException(
                    "Invalid connectionMethod. Use \"SSM\" (default, recommended) or \"SSH\".");

            using var scope = _scopeFactory.CreateScope();
            var accounts = scope.ServiceProvider.GetRequiredService<CloudAccountService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var account = await accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var instance = await db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == request.InstanceDbId
                    && x.CloudAccountId == accountId
                    && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            if (instance.State != "running")
                throw new InvalidOperationException(
                    $"EC2 instance is in '{instance.State}' state. It must be 'running' to connect.");

            var ip = !string.IsNullOrWhiteSpace(instance.PublicIpAddress)
                ? instance.PublicIpAddress
                : instance.PrivateIpAddress ?? string.Empty;

            var osUser = !string.IsNullOrWhiteSpace(request.OsUser)
                ? request.OsUser
                : InferOsUser(instance.Platform, instance.ImageId);

            if (method == "SSM")
                return await ConnectViaSsm(user, accountId, account, instance.InstanceId, ip, osUser);
            else
                return await ConnectViaSsh(user, accountId, account, db, instance, ip, osUser, request.PrivateKeyContent);
        }

        // ====================================================================
        // SSM CONNECT
        // ====================================================================

        private async Task<StartConnectionResponse> ConnectViaSsm(
            string user, int accountId, CloudConnectionSecrets account,
            string instanceId, string ip, string osUser)
        {
            _logger.LogInformation(
                "Attempting SSM connection for instance {InstanceId}...", instanceId);

            using var ssmClient = CreateSsmClient(account);

            // Verify the instance is managed by SSM (SSM agent online)
            var descResponse = await ssmClient.DescribeInstanceInformationAsync(
                new DescribeInstanceInformationRequest
                {
                    Filters =
                    [
                        new InstanceInformationStringFilter
                        {
                            Key = "InstanceIds",
                            Values = [instanceId]
                        }
                    ]
                });

            var ssmInfo = descResponse.InstanceInformationList.FirstOrDefault();

            if (ssmInfo is null || ssmInfo.PingStatus?.Value != "Online")
            {
                throw new InvalidOperationException(
                    $"EC2 instance '{instanceId}' is NOT registered with AWS Systems Manager (SSM) or the SSM agent is offline. " +
                    $"PREREQUISITES for SSM connection: " +
                    $"(1) The EC2 instance must have an IAM Instance Profile with the 'AmazonSSMManagedInstanceCore' policy attached. " +
                    $"(2) The SSM agent must be running on the instance (Amazon Linux 2 has it pre-installed). " +
                    $"(3) The instance must have outbound internet access to reach SSM endpoints. " +
                    $"FIX: Go to AWS Console → IAM → Roles → Create role → " +
                    $"Select 'AWS service' → Use case 'EC2' → Attach policy 'AmazonSSMManagedInstanceCore' → Create role. " +
                    $"Then go to EC2 → Select instance → Actions → Security → Modify IAM role → Attach the new role. " +
                    $"Wait 2-3 minutes for the SSM agent to register, then retry. " +
                    $"ALTERNATIVELY: Use connectionMethod='SSH' if you have port 22 open and a key pair with a stored private key.");
            }

            var sessionId = Guid.NewGuid().ToString("N");

            var session = new SshSession
            {
                SessionId = sessionId,
                User = user,
                AccountId = accountId,
                InstanceId = instanceId,
                IpAddress = ip,
                OsUser = osUser,
                ConnectionMethod = "SSM",
                ConnectedAt = DateTime.UtcNow,
                CurrentWorkingDirectory = $"/home/{osUser}",
                Client = null,
                ShellStream = null
            };

            _sessions[sessionId] = session;

            _logger.LogInformation(
                "SSM session {SessionId} established for instance {InstanceId}",
                sessionId, instanceId);

            return new StartConnectionResponse
            {
                SessionId = sessionId,
                Status = "Connected",
                ConnectionMethod = "SSM",
                InstanceId = instanceId,
                IpAddress = ip,
                OsUser = osUser,
                ConnectedAt = session.ConnectedAt
            };
        }

        // ====================================================================
        // SSH CONNECT (direct SSH — requires port 22 reachable)
        // ====================================================================

        private async Task<StartConnectionResponse> ConnectViaSsh(
            string user, int accountId, CloudConnectionSecrets account,
            AppDbContext db, CloudServices.EC2.Entities.Ec2InstanceRecord instance,
            string ip, string osUser, string? providedPrivateKey = null)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new InvalidOperationException(
                    "EC2 instance has no IP address. Ensure it is running and has a public or private IP assigned.");

            await ValidateSecurityGroupSshAccess(db, instance.SecurityGroupsJson, accountId, user, ip);

            // Resolve private key: use provided key first, then fall back to DB-stored key
            string? resolvedPrivateKey = null;
            string? resolvedPublicKey = null;

            if (!string.IsNullOrWhiteSpace(providedPrivateKey))
            {
                resolvedPrivateKey = providedPrivateKey.Trim();
                _logger.LogInformation(
                    "SSH: using caller-provided private key for instance {InstanceId}", instance.InstanceId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(instance.KeyName))
                    throw new InvalidOperationException(
                        "EC2 instance does not have a key pair attached. Cannot SSH without keys. " +
                        "Please paste your PEM private key in the connection dialog, or use connectionMethod='SSM'.");

                var keyPairRecord = await db.KeyPairRecords
                    .FirstOrDefaultAsync(x => x.KeyName == instance.KeyName
                        && x.CloudAccountId == accountId
                        && x.CreatedBy == user);

                if (keyPairRecord is null)
                    throw new KeyNotFoundException(
                        $"Key pair '{instance.KeyName}' not found in database. " +
                        "Please paste your PEM private key in the connection dialog, or use connectionMethod='SSM'.");

                if (string.IsNullOrWhiteSpace(keyPairRecord.PrivateKeyMaterial))
                    throw new InvalidOperationException(
                        $"Private key for '{instance.KeyName}' is not stored. " +
                        "Only key pairs created through this application have stored private keys. " +
                        "Please paste your PEM private key in the connection dialog, or use connectionMethod='SSM'.");

                resolvedPrivateKey = keyPairRecord.PrivateKeyMaterial;
                resolvedPublicKey = keyPairRecord.PublicKeyMaterial;
            }

            // Optionally push the public key via EC2 Instance Connect (only when using DB-stored key)
            if (!string.IsNullOrWhiteSpace(resolvedPublicKey))
            {
                try
                {
                    var connectProvider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                        ?? throw new InvalidOperationException("Cloud provider is not set."));

                    await connectProvider.SendSshPublicKey(
                        account, instance.InstanceId, osUser,
                        resolvedPublicKey, availabilityZone: null);

                    _logger.LogInformation(
                        "EC2 Instance Connect: Public key pushed for {InstanceId}", instance.InstanceId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "EC2 Instance Connect push failed for {InstanceId}. Attempting direct SSH.",
                        instance.InstanceId);
                }
            }

            var sessionId = Guid.NewGuid().ToString("N");
            SshClient? sshClient = null;
            ShellStream? shellStream = null;

            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(resolvedPrivateKey);
                using var keyStream = new MemoryStream(keyBytes);
                var privateKeyFile = new PrivateKeyFile(keyStream);

                var connectionInfo = new Renci.SshNet.ConnectionInfo(
                    ip, 22, osUser,
                    new PrivateKeyAuthenticationMethod(osUser, privateKeyFile))
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                sshClient = new SshClient(connectionInfo);
                _logger.LogInformation(
                    "Attempting SSH to {Ip}:22 as '{OsUser}' for {InstanceId}...",
                    ip, osUser, instance.InstanceId);

                sshClient.Connect();

                if (!sshClient.IsConnected)
                    throw new InvalidOperationException("SSH connection could not be established.");

                shellStream = sshClient.CreateShellStream("xterm", 200, 50, 800, 600, 4096);
                await Task.Delay(500);
                DrainShellStream(shellStream);

                var session = new SshSession
                {
                    SessionId = sessionId,
                    User = user,
                    AccountId = accountId,
                    InstanceId = instance.InstanceId,
                    IpAddress = ip,
                    OsUser = osUser,
                    ConnectionMethod = "SSH",
                    ConnectedAt = DateTime.UtcNow,
                    CurrentWorkingDirectory = $"/home/{osUser}",
                    Client = sshClient,
                    ShellStream = shellStream
                };

                _sessions[sessionId] = session;

                _logger.LogInformation(
                    "SSH session {SessionId} connected to {InstanceId} ({Ip})",
                    sessionId, instance.InstanceId, ip);

                return new StartConnectionResponse
                {
                    SessionId = sessionId,
                    Status = "Connected",
                    ConnectionMethod = "SSH",
                    InstanceId = instance.InstanceId,
                    IpAddress = ip,
                    OsUser = osUser,
                    ConnectedAt = session.ConnectedAt
                };
            }
            catch (SshConnectionException ex)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw new InvalidOperationException(
                    $"SSH connection refused by {ip}:22. Detail: {ex.Message}. " +
                    $"TIP: Use connectionMethod='SSM' instead.");
            }
            catch (SshAuthenticationException ex)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw new InvalidOperationException(
                    $"SSH auth failed for user '{osUser}' on {ip}. " +
                    $"Verify the OS user is correct (e.g. 'ec2-user' for Amazon Linux, 'ubuntu' for Ubuntu). " +
                    $"Detail: {ex.Message}");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw new InvalidOperationException(
                    $"Cannot reach {ip}:22 — connection timed out. " +
                    $"Your backend server cannot reach the EC2 instance over TCP port 22. " +
                    $"RECOMMENDED: Use connectionMethod='SSM' (does not need port 22). " +
                    $"Detail: {ex.Message}");
            }
            catch (Exception ex) when (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                                    || ex.Message.Contains("did not properly respond", StringComparison.OrdinalIgnoreCase)
                                    || ex.Message.Contains("15000 milliseconds", StringComparison.OrdinalIgnoreCase))
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw new InvalidOperationException(
                    $"Connection to {ip}:22 timed out. Your backend cannot reach the EC2 on port 22. " +
                    $"RECOMMENDED: Use connectionMethod='SSM' instead — it does not need SSH port 22 open. " +
                    $"If you must use SSH, ensure your VPC has an Internet Gateway, the route table has 0.0.0.0/0 → IGW, " +
                    $"and the security group allows inbound SSH from your IP.");
            }
            catch
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw;
            }
        }

        // ====================================================================
        // TAB COMPLETE — returns file/folder completions for the partial word
        // ====================================================================

        public async Task<List<string>> TabComplete(
            string user, int accountId, string sessionId, string partial)
        {
            var session = GetValidatedSession(user, accountId, sessionId);
            var cwd = session.CurrentWorkingDirectory;
            var cdPrefix = !string.IsNullOrWhiteSpace(cwd) ? $"cd {ShellEscape(cwd)} && " : "";
            // compgen -f lists files; compgen -d lists only dirs.
            // Using -f covers both files and directories.
            var safePartial = partial.Replace("'", "'\\''");
            var cmd = $"{cdPrefix}compgen -f -- '{safePartial}' 2>/dev/null; true";

            string stdout;
            if (session.ConnectionMethod == "SSM")
            {
                (stdout, _, _) = await ExecuteViaSsm(session, cmd);
            }
            else
            {
                if (session.Client is null || !session.Client.IsConnected)
                    return [];
                (stdout, _, _) = ExecuteViaSsh(session, cmd);
            }

            return stdout
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Take(50)   // cap results to avoid flooding
                .ToList();
        }

        // ====================================================================
        // CONNECT MANUAL — connect to any SSH host by providing details directly
        // ====================================================================

        public async Task<StartConnectionResponse> ConnectManual(
            string user, int accountId, ManualConnectionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IpAddress))
                throw new InvalidOperationException("IP address is required.");
            if (string.IsNullOrWhiteSpace(request.OsUser))
                throw new InvalidOperationException("OS username is required.");
            if (string.IsNullOrWhiteSpace(request.PrivateKeyContent))
                throw new InvalidOperationException("Private key (PEM) content is required.");

            var ip = request.IpAddress.Trim();
            var osUser = request.OsUser.Trim();
            var label = string.IsNullOrWhiteSpace(request.Label) ? ip : request.Label.Trim();
            var port = request.Port > 0 && request.Port <= 65535 ? request.Port : 22;

            var sessionId = Guid.NewGuid().ToString("N");
            SshClient? sshClient = null;
            ShellStream? shellStream = null;

            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(request.PrivateKeyContent);
                using var keyStream = new MemoryStream(keyBytes);
                var privateKeyFile = new PrivateKeyFile(keyStream);

                var connectionInfo = new Renci.SshNet.ConnectionInfo(
                    ip, port, osUser,
                    new PrivateKeyAuthenticationMethod(osUser, privateKeyFile))
                {
                    Timeout = TimeSpan.FromSeconds(20)
                };

                sshClient = new SshClient(connectionInfo);
                _logger.LogInformation(
                    "Manual SSH connect: attempting {OsUser}@{Ip}:{Port}", osUser, ip, port);

                sshClient.Connect();

                if (!sshClient.IsConnected)
                    throw new InvalidOperationException("SSH connection could not be established.");

                shellStream = sshClient.CreateShellStream("xterm", 200, 50, 800, 600, 4096);
                await Task.Delay(500);
                DrainShellStream(shellStream);

                var session = new SshSession
                {
                    SessionId = sessionId,
                    User = user,
                    AccountId = accountId,
                    InstanceId = label,          // use the label as the "instance ID" for display
                    IpAddress = ip,
                    OsUser = osUser,
                    ConnectionMethod = "SSH",
                    ConnectedAt = DateTime.UtcNow,
                    CurrentWorkingDirectory = $"/home/{osUser}",
                    Client = sshClient,
                    ShellStream = shellStream
                };

                _sessions[sessionId] = session;

                _logger.LogInformation(
                    "Manual SSH session {SessionId} connected to {Ip}:{Port} as '{OsUser}'",
                    sessionId, ip, port, osUser);

                return new StartConnectionResponse
                {
                    SessionId = sessionId,
                    Status = "Connected",
                    ConnectionMethod = "SSH",
                    InstanceId = label,
                    IpAddress = ip,
                    OsUser = osUser,
                    ConnectedAt = session.ConnectedAt
                };
            }
            catch (SshConnectionException ex)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw new InvalidOperationException(
                    $"SSH connection refused at {ip}:{port}. " +
                    $"Ensure the instance is running, port {port} is open, and the IP is correct. " +
                    $"Detail: {ex.Message}");
            }
            catch (SshAuthenticationException ex)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw new InvalidOperationException(
                    $"SSH authentication failed for user '{osUser}' at {ip}:{port}. " +
                    $"Verify the OS username and that the private key matches the instance's authorized key. " +
                    $"Detail: {ex.Message}");
            }
            catch (Exception)
            {
                shellStream?.Dispose();
                sshClient?.Dispose();
                throw;
            }
        }

        // ====================================================================
        // EXECUTE COMMAND — routes to SSM or SSH based on session method
        // ====================================================================

        public async Task<ExecuteCommandResponse> Execute(
            string user, int accountId, ExecuteCommandRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);

            var command = request.Command.Trim();
            if (string.IsNullOrWhiteSpace(command))
                throw new InvalidOperationException("Command cannot be empty.");

            // ── Build the wrapped command ──
            // We prefix with `cd <cwd>` so that every command runs in the tracked directory,
            // and append `; echo __CWD_MARKER__; pwd` so we can capture the resulting cwd.
            const string marker = "__CWD_MARKER__";
            var cwd = session.CurrentWorkingDirectory;
            var cdPrefix = !string.IsNullOrWhiteSpace(cwd) ? $"cd {ShellEscape(cwd)} && " : "";
            var wrappedCommand = $"{cdPrefix}{command}; __exit=$?; echo '{marker}'; pwd; exit $__exit";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
            {
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, wrappedCommand);
            }
            else
            {
                if (session.Client is null || !session.Client.IsConnected)
                {
                    _sessions.TryRemove(request.SessionId, out _);
                    session.Dispose();
                    throw new InvalidOperationException(
                        "SSH session is no longer connected. Please start a new session.");
                }

                (stdout, stderr, exitCode) = ExecuteViaSsh(session, wrappedCommand);
            }

            // ── Parse out the cwd from the output ──
            var cleanOutput = stdout;
            var markerIdx = stdout.LastIndexOf(marker, StringComparison.Ordinal);
            if (markerIdx >= 0)
            {
                cleanOutput = stdout.Substring(0, markerIdx).TrimEnd('\n', '\r');
                var afterMarker = stdout.Substring(markerIdx + marker.Length).Trim();
                if (!string.IsNullOrWhiteSpace(afterMarker))
                {
                    // afterMarker is the pwd output (possibly with trailing newlines)
                    var newCwd = afterMarker.Split('\n')[0].Trim();
                    if (newCwd.StartsWith("/") || newCwd.StartsWith("~"))
                        session.CurrentWorkingDirectory = newCwd;
                }
            }

            var logEntry = new CommandLogEntry
            {
                Command = command,
                StandardOutput = cleanOutput.TrimEnd(),
                StandardError = stderr.TrimEnd(),
                ExitCode = exitCode,
                ExecutedAt = DateTime.UtcNow
            };

            session.CommandHistory.Add(logEntry);

            _logger.LogInformation(
                "Session {SessionId} [{Method}]: '{Command}' (exit={ExitCode}, cwd={Cwd})",
                request.SessionId, session.ConnectionMethod, command, exitCode,
                session.CurrentWorkingDirectory);

            return new ExecuteCommandResponse
            {
                SessionId = request.SessionId,
                Command = command,
                StandardOutput = logEntry.StandardOutput,
                StandardError = logEntry.StandardError,
                ExitCode = exitCode,
                WorkingDirectory = session.CurrentWorkingDirectory,
                ExecutedAt = logEntry.ExecutedAt
            };
        }

        // ---- SSM Execute ----

        private async Task<(string stdout, string stderr, int exitCode)> ExecuteViaSsm(
            SshSession session, string command)
        {
            using var scope = _scopeFactory.CreateScope();
            var accounts = scope.ServiceProvider.GetRequiredService<CloudAccountService>();

            var account = await accounts.ResolveCredentialsAsync(session.User, session.AccountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            using var ssmClient = CreateSsmClient(account);

            SendCommandResponse sendResponse;
            try
            {
                sendResponse = await ssmClient.SendCommandAsync(new SendCommandRequest
                {
                    InstanceIds = [session.InstanceId],
                    DocumentName = "AWS-RunShellScript",
                    Parameters = new Dictionary<string, List<string>>
                    {
                        { "commands", [command] }
                    },
                    TimeoutSeconds = 60
                });
            }
            catch (Amazon.SimpleSystemsManagement.Model.InvalidInstanceIdException)
            {
                throw new InvalidOperationException(
                    $"SSM cannot reach instance '{session.InstanceId}'. " +
                    "Ensure the IAM Instance Profile with 'AmazonSSMManagedInstanceCore' is attached and the SSM agent is running.");
            }

            var commandId = sendResponse.Command.CommandId;

            // Poll for completion
            GetCommandInvocationResponse? result = null;
            int retries = 0;
            const int maxRetries = 60;

            do
            {
                await Task.Delay(1000);
                try
                {
                    result = await ssmClient.GetCommandInvocationAsync(new GetCommandInvocationRequest
                    {
                        CommandId = commandId,
                        InstanceId = session.InstanceId
                    });
                }
                catch (InvocationDoesNotExistException) when (retries < 5)
                {
                    // SSM may not have registered the invocation yet — retry
                    retries++;
                    continue;
                }

                if (result is not null && result.Status?.Value is not ("Pending" or "InProgress" or "Delayed"))
                    break;

                retries++;
            }
            while (retries < maxRetries);

            if (retries >= maxRetries || result is null)
                throw new InvalidOperationException(
                    $"Command execution timed out after {maxRetries} seconds on instance '{session.InstanceId}'.");

            var status = result.Status?.Value ?? "Unknown";
            if (status == "Failed" && string.IsNullOrWhiteSpace(result.StandardOutputContent)
                                   && string.IsNullOrWhiteSpace(result.StandardErrorContent))
            {
                throw new InvalidOperationException(
                    $"SSM command failed with status '{status}'. " +
                    $"StatusDetails: {result.StatusDetails ?? "none"}. " +
                    "The SSM agent may not be responding. Check the instance's IAM role and SSM agent status.");
            }

            return (
                result.StandardOutputContent ?? string.Empty,
                result.StandardErrorContent ?? string.Empty,
                result.ResponseCode ?? -1
            );
        }

        // ---- SSH Execute ----

        private (string stdout, string stderr, int exitCode) ExecuteViaSsh(
            SshSession session, string command)
        {
            try
            {
                using var cmd = session.Client!.RunCommand(command);
                return (
                    cmd.Result ?? string.Empty,
                    cmd.Error ?? string.Empty,
                    cmd.ExitStatus ?? 0
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSH command execution failed in session {SessionId}", session.SessionId);
                throw new InvalidOperationException($"Command execution failed: {ex.Message}");
            }
        }

        // ====================================================================
        // DISCONNECT
        // ====================================================================

        public Task<DisconnectResponse> Disconnect(string user, int accountId, string sessionId)
        {
            var session = GetValidatedSession(user, accountId, sessionId);

            _sessions.TryRemove(sessionId, out _);
            session.Dispose();

            _logger.LogInformation("Session {SessionId} [{Method}] disconnected.",
                sessionId, session.ConnectionMethod);

            return Task.FromResult(new DisconnectResponse
            {
                SessionId = sessionId,
                Status = "Disconnected",
                DisconnectedAt = DateTime.UtcNow
            });
        }

        // ====================================================================
        // SESSION STATUS
        // ====================================================================

        public Task<SessionStatusResponse> GetSessionStatus(string user, int accountId, string sessionId)
        {
            var session = GetValidatedSession(user, accountId, sessionId);

            return Task.FromResult(new SessionStatusResponse
            {
                SessionId = session.SessionId,
                Status = session.IsConnected ? "Connected" : "Disconnected",
                ConnectionMethod = session.ConnectionMethod,
                InstanceId = session.InstanceId,
                IpAddress = session.IpAddress,
                OsUser = session.OsUser,
                ConnectedAt = session.ConnectedAt,
                CommandHistory = session.CommandHistory
            });
        }

        // ====================================================================
        // LIST ACTIVE SESSIONS
        // ====================================================================

        public Task<ActiveSessionsListResponse> ListActiveSessions(string user, int accountId)
        {
            var sessions = _sessions.Values
                .Where(s => s.User == user && s.AccountId == accountId)
                .Select(s => new ActiveSessionInfo
                {
                    SessionId = s.SessionId,
                    ConnectionMethod = s.ConnectionMethod,
                    InstanceId = s.InstanceId,
                    IpAddress = s.IpAddress,
                    OsUser = s.OsUser,
                    Status = s.IsConnected ? "Connected" : "Disconnected",
                    CommandCount = s.CommandHistory.Count,
                    ConnectedAt = s.ConnectedAt
                })
                .OrderByDescending(s => s.ConnectedAt)
                .ToList();

            return Task.FromResult(new ActiveSessionsListResponse { Sessions = sessions });
        }

        // ====================================================================
        // PRIVATE HELPERS
        // ====================================================================

        private static SshSession GetValidatedSession(string user, int accountId, string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            if (session.User != user || session.AccountId != accountId)
                throw new UnauthorizedAccessException("You do not have access to this session.");

            return session;
        }

        private static AmazonSimpleSystemsManagementClient CreateSsmClient(CloudConnectionSecrets account)
        {
            return new AmazonSimpleSystemsManagementClient(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region));
        }

        private static string InferOsUser(string platform, string imageId)
        {
            var p = (platform ?? string.Empty).ToLowerInvariant();
            var img = (imageId ?? string.Empty).ToLowerInvariant();

            if (p.Contains("windows")) return "Administrator";
            if (p.Contains("ubuntu") || img.Contains("ubuntu")) return "ubuntu";
            if (p.Contains("debian") || img.Contains("debian")) return "admin";
            if (p.Contains("centos") || img.Contains("centos")) return "centos";
            if (p.Contains("fedora") || img.Contains("fedora")) return "fedora";
            if (p.Contains("bitnami") || img.Contains("bitnami")) return "bitnami";
            return "ec2-user";
        }

        private static async Task ValidateSecurityGroupSshAccess(
            AppDbContext db, string? securityGroupsJson, int accountId, string user, string connectingToIp)
        {
            if (string.IsNullOrWhiteSpace(securityGroupsJson)) return;

            List<EC2.DTOs.Ec2SecurityGroupDto>? sgList;
            try { sgList = JsonSerializer.Deserialize<List<EC2.DTOs.Ec2SecurityGroupDto>>(securityGroupsJson); }
            catch { return; }

            if (sgList is null || sgList.Count == 0) return;

            var sgIds = sgList.Select(s => s.GroupId).ToList();
            var sgRecords = await db.SecurityGroupRecords
                .Where(x => sgIds.Contains(x.SecurityGroupId) && x.CloudAccountId == accountId)
                .ToListAsync();

            if (sgRecords.Count == 0) return;

            bool sshAllowed = false;
            foreach (var sg in sgRecords)
            {
                if (string.IsNullOrWhiteSpace(sg.InboundRulesJson)) continue;

                List<SecurityGroupRuleDto>? rules;
                try { rules = JsonSerializer.Deserialize<List<SecurityGroupRuleDto>>(sg.InboundRulesJson); }
                catch { continue; }
                if (rules is null) continue;

                foreach (var rule in rules)
                {
                    bool protocolMatch = rule.Protocol == "-1"
                        || rule.Protocol.Equals("tcp", StringComparison.OrdinalIgnoreCase);
                    bool portMatch = (rule.FromPort == -1 && rule.ToPort == -1)
                        || (rule.FromPort <= 22 && rule.ToPort >= 22);
                    bool sourceMatch = rule.Ipv4Ranges.Any(r => r == "0.0.0.0/0")
                        || rule.Ipv6Ranges.Any(r => r == "::/0")
                        || rule.Ipv4Ranges.Count > 0
                        || rule.ReferencedGroupIds.Count > 0;

                    if (protocolMatch && portMatch && sourceMatch)
                    { sshAllowed = true; break; }
                }
                if (sshAllowed) break;
            }

            if (!sshAllowed)
            {
                var sgNames = string.Join(", ", sgList.Select(s => $"{s.GroupName} ({s.GroupId})"));
                throw new InvalidOperationException(
                    $"SSH port 22 is NOT open in security group(s): {sgNames}. " +
                    $"Use connectionMethod='SSM' instead, or add an inbound SSH rule.");
            }
        }

        private static string DrainShellStream(ShellStream stream)
        {
            var sb = new StringBuilder();
            while (stream.DataAvailable) sb.Append(stream.Read());
            return sb.ToString();
        }

        // ====================================================================
        // FILE BROWSER — LIST DIRECTORY
        // ====================================================================

        public async Task<FileListResponse> ListDirectory(
            string user, int accountId, string sessionId, string path)
        {
            var session = GetValidatedSession(user, accountId, sessionId);
            var safePath = SanitizePath(path);

            // Try Python3 first (structured JSON), fall back to ls parsing
            var pyCmd = $"python3 -c \""
                + $"import os,json,stat as s; d='{safePath}'; entries=[];"
                + $"[entries.append({{'name':e.name,'full_path':e.path,'is_dir':e.is_dir(),'is_link':e.is_symlink(),"
                + $"'size':0 if e.is_dir() else e.stat(follow_symlinks=False).st_size,"
                + $"'permissions':oct(s.S_IMODE(e.stat(follow_symlinks=False).st_mode))[-4:],"
                + $"'owner':str(e.stat(follow_symlinks=False).st_uid),'group':str(e.stat(follow_symlinks=False).st_gid),"
                + $"'mtime':e.stat(follow_symlinks=False).st_mtime,"
                + $"'link_target':os.readlink(e.path) if e.is_symlink() else ''}}) for e in sorted(os.scandir(d),key=lambda x:(not x.is_dir(),x.name.lower()))];"
                + $"print(json.dumps(entries))"
                + $"\" 2>&1";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, pyCmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, pyCmd);

            var parentPath = safePath == "/" ? "/" : safePath.TrimEnd('/').Contains('/')
                ? safePath.TrimEnd('/').Substring(0, safePath.TrimEnd('/').LastIndexOf('/') + 1)
                : "/";
            if (string.IsNullOrWhiteSpace(parentPath)) parentPath = "/";

            // If Python3 works, parse JSON
            if (exitCode == 0 && stdout.TrimStart().StartsWith("["))
            {
                try
                {
                    var pyEntries = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(stdout.Trim());
                    var entries = pyEntries!.Select(e => new FileEntryInfo
                    {
                        Name = e.GetProperty("name").GetString() ?? string.Empty,
                        FullPath = e.GetProperty("full_path").GetString() ?? string.Empty,
                        IsDirectory = e.GetProperty("is_dir").GetBoolean(),
                        IsSymlink = e.GetProperty("is_link").GetBoolean(),
                        Size = e.GetProperty("size").GetInt64(),
                        Permissions = e.GetProperty("permissions").GetString() ?? string.Empty,
                        Owner = e.GetProperty("owner").GetString() ?? string.Empty,
                        Group = e.GetProperty("group").GetString() ?? string.Empty,
                        ModifiedAt = DateTimeOffset.FromUnixTimeSeconds((long)e.GetProperty("mtime").GetDouble()).UtcDateTime.ToString("o"),
                        LinkTarget = e.GetProperty("link_target").GetString() ?? string.Empty,
                        Extension = System.IO.Path.GetExtension(e.GetProperty("name").GetString() ?? string.Empty)
                    }).ToList();

                    return new FileListResponse
                    {
                        CurrentPath = safePath,
                        ParentPath = parentPath,
                        Entries = entries
                    };
                }
                catch { /* fall through to ls parsing */ }
            }

            // Fallback: parse ls -la output
            var lsCmd = $"ls -la --color=never --time-style=\"+%Y-%m-%dT%H:%M:%S\" '{safePath}' 2>&1";
            string lsOut, lsErr;
            int lsExit;

            if (session.ConnectionMethod == "SSM")
                (lsOut, lsErr, lsExit) = await ExecuteViaSsm(session, lsCmd);
            else
                (lsOut, lsErr, lsExit) = ExecuteViaSsh(session, lsCmd);

            if (lsExit != 0)
                throw new InvalidOperationException($"Cannot list directory '{safePath}': {lsOut} {lsErr}".Trim());

            var parsedEntries = ParseLsOutput(lsOut, safePath);
            return new FileListResponse
            {
                CurrentPath = safePath,
                ParentPath = parentPath,
                Entries = parsedEntries
            };
        }

        private static List<FileEntryInfo> ParseLsOutput(string lsOutput, string basePath)
        {
            var entries = new List<FileEntryInfo>();
            var lines = lsOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Regex: permissions links owner group size date name [-> target]
            var regex = new System.Text.RegularExpressions.Regex(
                @"^([dlcbsp-][rwxsStT-]{9}[+@.]?\s+)\s*\d+\s+(\S+)\s+(\S+)\s+(\d+)\s+(\S+)\s+(.+?)(?:\s+->\s+(.*))?$");

            foreach (var line in lines)
            {
                if (line.StartsWith("total") || line.StartsWith("//")) continue;

                var m = regex.Match(line.Trim());
                if (!m.Success) continue;

                var perms = m.Groups[1].Value.Trim();
                var owner = m.Groups[2].Value;
                var group = m.Groups[3].Value;
                var size = long.TryParse(m.Groups[4].Value, out var s) ? s : 0;
                var modified = m.Groups[5].Value;
                var name = m.Groups[6].Value.Trim();
                var linkTarget = m.Groups[7].Value.Trim();

                if (name == "." || name == "..") continue;

                var isDir = perms.StartsWith("d");
                var isLink = perms.StartsWith("l");
                var fullPath = basePath.TrimEnd('/') + "/" + name;

                entries.Add(new FileEntryInfo
                {
                    Name = name,
                    FullPath = fullPath,
                    IsDirectory = isDir,
                    IsSymlink = isLink,
                    Size = size,
                    Permissions = perms.Length > 1 ? perms.Substring(1) : perms,
                    Owner = owner,
                    Group = group,
                    ModifiedAt = modified,
                    LinkTarget = linkTarget,
                    Extension = isDir ? string.Empty : System.IO.Path.GetExtension(name)
                });
            }

            // Directories first, then files alphabetically
            return entries
                .OrderBy(e => e.IsDirectory ? 0 : 1)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ====================================================================
        // FILE BROWSER — READ FILE
        // ====================================================================

        public async Task<FileReadResponse> ReadFile(
            string user, int accountId, string sessionId, string path)
        {
            var session = GetValidatedSession(user, accountId, sessionId);
            var safePath = SanitizePath(path);

            // Get file size first
            string sizeOut, sizeErr;
            int sizeExit;
            var sizeCmd = $"stat -c '%s' '{safePath}' 2>&1";
            if (session.ConnectionMethod == "SSM")
                (sizeOut, sizeErr, sizeExit) = await ExecuteViaSsm(session, sizeCmd);
            else
                (sizeOut, sizeErr, sizeExit) = ExecuteViaSsh(session, sizeCmd);

            long fileSize = long.TryParse(sizeOut.Trim(), out var fs) ? fs : 0;

            const long MaxReadSize = 1 * 1024 * 1024; // 1 MB text limit
            const long MaxBinarySize = 5 * 1024 * 1024; // 5 MB binary limit

            if (fileSize > MaxBinarySize)
                throw new InvalidOperationException(
                    $"File is too large ({fileSize / 1024 / 1024} MB). Maximum allowed is 5 MB.");

            // Read as base64 to safely handle any encoding
            var readCmd = $"base64 -w 0 '{safePath}' 2>&1";
            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, readCmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, readCmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot read file '{safePath}': {stdout} {stderr}".Trim());

            byte[] rawBytes;
            try { rawBytes = Convert.FromBase64String(stdout.Trim()); }
            catch { rawBytes = Encoding.UTF8.GetBytes(stdout.Trim()); }

            bool isBinary = IsBinaryContent(rawBytes, Math.Min(rawBytes.Length, 8192));

            string content;
            if (!isBinary && fileSize <= MaxReadSize)
                content = Encoding.UTF8.GetString(rawBytes);
            else if (!isBinary)
                content = Encoding.UTF8.GetString(rawBytes, 0, (int)MaxReadSize) + "\n... (truncated)";
            else
                content = Convert.ToBase64String(rawBytes); // Send as base64 for binary files

            return new FileReadResponse
            {
                Path = safePath,
                Content = content,
                IsBinary = isBinary,
                Size = fileSize,
                Encoding = isBinary ? "base64" : "utf-8"
            };
        }

        // ====================================================================
        // FILE BROWSER — WRITE FILE
        // ====================================================================

        public async Task<FileOperationResponse> WriteFile(
            string user, int accountId, FileWriteRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);
            var safePath = SanitizePath(request.Path);

            // Encode content as base64 to avoid shell escaping issues
            var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Content));
            var op = request.Append ? ">>" : ">";
            var writeCmd = $"echo '{base64Content}' | base64 -d {op} '{safePath}' 2>&1";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, writeCmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, writeCmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot write file '{safePath}': {stdout} {stderr}".Trim());

            return new FileOperationResponse { Success = true, Message = $"File '{safePath}' saved successfully." };
        }

        // ====================================================================
        // FILE BROWSER — DELETE
        // ====================================================================

        public async Task<FileOperationResponse> DeleteFileOrDirectory(
            string user, int accountId, FileDeleteRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);
            var safePath = SanitizePath(request.Path);

            // Safety: never allow deletion of root or critical paths
            var forbiddenPaths = new[] { "/", "/etc", "/usr", "/bin", "/sbin", "/lib", "/lib64", "/boot", "/sys", "/proc", "/dev" };
            if (forbiddenPaths.Any(f => safePath == f))
                throw new InvalidOperationException($"Deletion of '{safePath}' is forbidden for safety reasons.");

            var flag = request.Recursive ? "-rf" : "-f";
            var cmd = $"rm {flag} '{safePath}' 2>&1";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, cmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, cmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot delete '{safePath}': {stdout} {stderr}".Trim());

            return new FileOperationResponse { Success = true, Message = $"'{safePath}' deleted successfully." };
        }

        // ====================================================================
        // FILE BROWSER — MAKE DIRECTORY
        // ====================================================================

        public async Task<FileOperationResponse> MakeDirectory(
            string user, int accountId, FileMkdirRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);
            var safePath = SanitizePath(request.Path);
            var flag = request.CreateParents ? "-p" : string.Empty;
            var cmd = $"mkdir {flag} '{safePath}' 2>&1";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, cmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, cmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot create directory '{safePath}': {stdout} {stderr}".Trim());

            return new FileOperationResponse { Success = true, Message = $"Directory '{safePath}' created successfully." };
        }

        // ====================================================================
        // FILE BROWSER — RENAME / MOVE
        // ====================================================================

        public async Task<FileOperationResponse> RenameOrMove(
            string user, int accountId, FileRenameRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);
            var oldPath = SanitizePath(request.OldPath);
            var newPath = SanitizePath(request.NewPath);
            var cmd = $"mv '{oldPath}' '{newPath}' 2>&1";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, cmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, cmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot rename/move '{oldPath}' to '{newPath}': {stdout} {stderr}".Trim());

            return new FileOperationResponse { Success = true, Message = $"Renamed to '{newPath}' successfully." };
        }

        // ====================================================================
        // FILE BROWSER — COPY
        // ====================================================================

        public async Task<FileOperationResponse> CopyFile(
            string user, int accountId, FileCopyRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);
            var src = SanitizePath(request.SourcePath);
            var dst = SanitizePath(request.DestinationPath);
            var cmd = $"cp -r '{src}' '{dst}' 2>&1";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, cmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, cmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot copy '{src}' to '{dst}': {stdout} {stderr}".Trim());

            return new FileOperationResponse { Success = true, Message = $"Copied to '{dst}' successfully." };
        }

        // ====================================================================
        // FILE BROWSER — DOWNLOAD (returns base64-encoded bytes)
        // ====================================================================

        public async Task<FileDownloadResponse> DownloadFile(
            string user, int accountId, string sessionId, string path)
        {
            var session = GetValidatedSession(user, accountId, sessionId);
            var safePath = SanitizePath(path);

            var sizeCmd = $"stat -c '%s' '{safePath}' 2>&1";
            string sizeOut, sizeErr;
            int sizeExit;
            if (session.ConnectionMethod == "SSM")
                (sizeOut, sizeErr, sizeExit) = await ExecuteViaSsm(session, sizeCmd);
            else
                (sizeOut, sizeErr, sizeExit) = ExecuteViaSsh(session, sizeCmd);

            long fileSize = long.TryParse(sizeOut.Trim(), out var fs) ? fs : 0;

            const long MaxDownloadSize = 50 * 1024 * 1024; // 50 MB
            if (fileSize > MaxDownloadSize)
                throw new InvalidOperationException($"File is too large ({fileSize / 1024 / 1024} MB) to download via this endpoint.");

            var readCmd = $"base64 -w 0 '{safePath}' 2>&1";
            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, readCmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, readCmd);

            if (exitCode != 0)
                throw new InvalidOperationException($"Cannot read file '{safePath}': {stdout} {stderr}".Trim());

            var fileName = System.IO.Path.GetFileName(safePath);
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".txt" or ".log" or ".sh" or ".conf" or ".cfg" or ".yml" or ".yaml" or ".json" or ".xml" or ".csv" => "text/plain",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                _ => "application/octet-stream"
            };

            return new FileDownloadResponse
            {
                Path = safePath,
                FileName = fileName,
                ContentBase64 = stdout.Trim(),
                Size = fileSize,
                MimeType = mimeType
            };
        }

        // ====================================================================
        // FILE BROWSER — SEARCH FILES
        // ====================================================================

        public async Task<FileSearchResponse> SearchFiles(
            string user, int accountId, FileSearchRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);
            var safeDir = SanitizePath(request.Directory);
            var safePattern = request.Pattern.Replace("'", "\\'");
            var limit = Math.Min(request.MaxResults, 200);
            var caseFlag = request.CaseSensitive ? string.Empty : "-iname";
            if (!request.CaseSensitive) caseFlag = "-iname";
            else caseFlag = "-name";

            var cmd = $"find '{safeDir}' {caseFlag} '*{safePattern}*' -not -path '*/.*' 2>/dev/null | head -{limit}";

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, cmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, cmd);

            var results = new List<FileEntryInfo>();
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var fp = line.Trim();
                    var name = System.IO.Path.GetFileName(fp);
                    results.Add(new FileEntryInfo
                    {
                        Name = name,
                        FullPath = fp,
                        IsDirectory = false,
                        Extension = System.IO.Path.GetExtension(name)
                    });
                }
            }

            return new FileSearchResponse
            {
                Directory = safeDir,
                Pattern = request.Pattern,
                Results = results,
                TotalFound = results.Count
            };
        }

        // ====================================================================
        // SYSTEM INFO
        // ====================================================================

        public async Task<SystemInfoResponse> GetSystemInfo(string user, int accountId, string sessionId)
        {
            var session = GetValidatedSession(user, accountId, sessionId);

            var scriptLines = new[]
            {
                "echo \"HOSTNAME=$(hostname)\"",
                "echo \"KERNEL=$(uname -r)\"",
                "echo \"OS=$(cat /etc/os-release 2>/dev/null | grep PRETTY_NAME | cut -d= -f2 | tr -d '\"' || uname -s)\"",
                "echo \"UPTIME=$(uptime -p 2>/dev/null || uptime)\"",
                "echo \"CPU=$(nproc) vCPU(s)\"",
                "echo \"MEMTOTAL=$(grep MemTotal /proc/meminfo | awk '{print $2 \" kB\"}')\"",
                "echo \"MEMFREE=$(grep MemAvailable /proc/meminfo | awk '{print $2 \" kB\"}')\"",
                "echo \"DISK=$(df -h / | tail -1 | awk '{print $3\"/\"$2\" used (\"$5\" full)\"}')\"",
                "echo \"HOME=$(eval echo ~${USER:-$(whoami)})\"",
                "echo \"USER=$(whoami)\"",
                "echo \"SHELL=$(echo $SHELL)\"",
                "echo \"PWD=$(pwd)\""
            };

            var cmd = string.Join(" && ", scriptLines);

            string stdout, stderr;
            int exitCode;

            if (session.ConnectionMethod == "SSM")
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, cmd);
            else
                (stdout, stderr, exitCode) = ExecuteViaSsh(session, cmd);

            var info = new SystemInfoResponse { SessionId = sessionId };
            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = line.IndexOf('=');
                if (idx < 0) continue;
                var key = line[..idx];
                var val = line[(idx + 1)..].Trim();
                switch (key)
                {
                    case "HOSTNAME": info.Hostname = val; break;
                    case "KERNEL": info.OsRelease = val; break;
                    case "OS": info.Kernel = val; break;
                    case "UPTIME": info.Uptime = val; break;
                    case "CPU": info.Cpu = val; break;
                    case "MEMTOTAL": info.Memory = $"Total: {val}"; break;
                    case "MEMFREE": info.Memory += $" | Available: {val}"; break;
                    case "DISK": info.DiskUsage = val; break;
                    case "HOME": info.HomeDirectory = val; break;
                    case "USER": info.RemoteUser = val; break;
                    case "SHELL": info.Shell = val; break;
                    case "PWD": info.WorkingDirectory = val; break;
                }
            }

            return info;
        }

        // ====================================================================
        // PRIVATE HELPERS (file browser)
        // ====================================================================

        private static string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            // Remove null bytes and dangerous patterns
            path = path.Replace("\0", "").Trim();
            // Normalize multiple slashes
            while (path.Contains("//")) path = path.Replace("//", "/");
            // Must start with /
            if (!path.StartsWith("/")) path = "/" + path;
            return path;
        }

        /// <summary>Wraps a value in single quotes for safe shell usage.</summary>
        private static string ShellEscape(string value)
        {
            // Replace ' with '\'' (end quote, escaped single quote, start quote)
            return "'" + value.Replace("'", "'\\''") + "'";
        }

        private static bool IsBinaryContent(byte[] bytes, int length)
        {
            int nullCount = 0;
            int controlCount = 0;
            for (int i = 0; i < length && i < bytes.Length; i++)
            {
                if (bytes[i] == 0) nullCount++;
                else if (bytes[i] < 8 || (bytes[i] > 13 && bytes[i] < 32)) controlCount++;
            }
            return nullCount > 0 || (controlCount > length * 0.10);
        }

        // ====================================================================
        // CLEANUP
        // ====================================================================

        public void Dispose()
        {
            foreach (var kvp in _sessions) kvp.Value.Dispose();
            _sessions.Clear();
        }
    }
}
