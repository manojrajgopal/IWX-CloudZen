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
                return await ConnectViaSsh(user, accountId, account, db, instance, ip, osUser);
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
            string ip, string osUser)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new InvalidOperationException(
                    "EC2 instance has no IP address. Ensure it is running and has a public or private IP assigned.");

            await ValidateSecurityGroupSshAccess(db, instance.SecurityGroupsJson, accountId, user, ip);

            if (string.IsNullOrWhiteSpace(instance.KeyName))
                throw new InvalidOperationException(
                    "EC2 instance does not have a key pair attached. Cannot SSH without keys. " +
                    "TIP: Use connectionMethod='SSM' instead — it does not require a key pair.");

            var keyPairRecord = await db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.KeyName == instance.KeyName
                    && x.CloudAccountId == accountId
                    && x.CreatedBy == user)
                ?? throw new KeyNotFoundException(
                    $"Key pair '{instance.KeyName}' not found in database. " +
                    "Please sync your key pairs using the KeyPair sync API, or use connectionMethod='SSM'.");

            if (string.IsNullOrWhiteSpace(keyPairRecord.PrivateKeyMaterial))
                throw new InvalidOperationException(
                    $"Private key for '{instance.KeyName}' is not available. " +
                    "Only key pairs created through this API have stored private keys. " +
                    "TIP: Use connectionMethod='SSM' instead — it does not require a private key.");

            // Optionally push the public key via EC2 Instance Connect
            if (!string.IsNullOrWhiteSpace(keyPairRecord.PublicKeyMaterial))
            {
                try
                {
                    var connectProvider = Ec2InstanceConnectProviderFactory.Get(account.Provider
                        ?? throw new InvalidOperationException("Cloud provider is not set."));

                    await connectProvider.SendSshPublicKey(
                        account, instance.InstanceId, osUser,
                        keyPairRecord.PublicKeyMaterial, availabilityZone: null);

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
                var keyBytes = Encoding.UTF8.GetBytes(keyPairRecord.PrivateKeyMaterial);
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
        // EXECUTE COMMAND — routes to SSM or SSH based on session method
        // ====================================================================

        public async Task<ExecuteCommandResponse> Execute(
            string user, int accountId, ExecuteCommandRequest request)
        {
            var session = GetValidatedSession(user, accountId, request.SessionId);

            var command = request.Command.Trim();
            if (string.IsNullOrWhiteSpace(command))
                throw new InvalidOperationException("Command cannot be empty.");

            string stdout, stderr;
            int exitCode;
            string workingDirectory;

            if (session.ConnectionMethod == "SSM")
            {
                (stdout, stderr, exitCode) = await ExecuteViaSsm(session, command);
                workingDirectory = string.Empty; // SSM doesn't track cwd across calls
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

                (stdout, stderr, exitCode) = ExecuteViaSsh(session, command);
                workingDirectory = GetWorkingDirectorySsh(session);
            }

            var logEntry = new CommandLogEntry
            {
                Command = command,
                StandardOutput = stdout.TrimEnd(),
                StandardError = stderr.TrimEnd(),
                ExitCode = exitCode,
                ExecutedAt = DateTime.UtcNow
            };

            session.CommandHistory.Add(logEntry);

            _logger.LogInformation(
                "Session {SessionId} [{Method}]: '{Command}' (exit={ExitCode})",
                request.SessionId, session.ConnectionMethod, command, exitCode);

            return new ExecuteCommandResponse
            {
                SessionId = request.SessionId,
                Command = command,
                StandardOutput = logEntry.StandardOutput,
                StandardError = logEntry.StandardError,
                ExitCode = exitCode,
                WorkingDirectory = workingDirectory,
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

        private static string GetWorkingDirectorySsh(SshSession session)
        {
            if (session.Client is null || !session.Client.IsConnected) return string.Empty;
            try
            {
                using var cmd = session.Client.RunCommand("pwd");
                return cmd.Result?.Trim() ?? string.Empty;
            }
            catch { return string.Empty; }
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
        // CLEANUP
        // ====================================================================

        public void Dispose()
        {
            foreach (var kvp in _sessions) kvp.Value.Dispose();
            _sessions.Clear();
        }
    }
}
