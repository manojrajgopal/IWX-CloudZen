using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.SecurityToken;
using IWX_CloudZen.CloudAccounts.DTOs;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace IWX_CloudZen.CloudDeployments.Pipeline
{
    public class EcrPushService
    {
        private static string NormalizeAwsName(string name)
        {
            name = name.Trim().ToLowerInvariant();
            name = Regex.Replace(name, @"[^a-z0-9._/-]", "-");
            name = Regex.Replace(name, @"[-._/]{2,}", "-");
            return name.Trim('-', '.', '_', '/');
        }

        private static void CreateDockerfile(string root, string deploymentType)
        {
            var lower = (deploymentType ?? string.Empty).Trim().ToLowerInvariant();
            var content = lower.Contains("container")
                ? """
                  FROM alpine:latest
                  CMD ["sh", "-c", "echo Container image requires a Dockerfile in your uploaded package && sleep infinity"]
                  """ : """
                  FROM python:3.11-slim
                  WORKDIR /app
                  COPY . /app
                  RUN pip install --no-cache-dir --upgrade pip && \
                      if [ -f requirements.txt ]; then pip install --no-cache-dir -r requirements.txt; else pip install --no-cache-dir flask gunicorn; fi
                  EXPOSE 80
                  CMD ["sh", "-c", "if [ -f app.py ]; then gunicorn -b 0.0.0.0:80 app:app || python app.py; else python -m http.server 80; fi"]
                  """;

            File.WriteAllText(Path.Combine(root, "Dockerfile"), content);
        }

        private static async Task EnsureDockerRunning()
        {
            static async Task<bool> TryDockerInfoAsync()
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return false;

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }

            if (await TryDockerInfoAsync())
                return;

            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Docker", "Docker", "Docker Desktop.exe")
            };

            var dockerDesktop = possiblePaths.FirstOrDefault(File.Exists);

            if (dockerDesktop == null)
                throw new Exception("Docker Desktop is not installed.");

            Process.Start(new ProcessStartInfo
            {
                FileName = dockerDesktop,
                UseShellExecute = true
            });

            var timeout = DateTime.UtcNow.AddMinutes(2);

            while (DateTime.UtcNow < timeout)
            {
                await Task.Delay(3000);

                if (await TryDockerInfoAsync())
                    return;
            }

            throw new Exception("Docker Desktop started, but Docker engine is still not ready.");
        }

        private static async Task RunProcessAsync(string fileName, string arguments, string? stdin = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new Exception($"Failed to start {fileName}");

            if (!string.IsNullOrWhiteSpace(stdin))
            {
                await process.StandardInput.WriteLineAsync(stdin);
            }

            process.StandardInput.Close();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"{fileName} failed: {error}\n{output}");
        }

        public async Task<(string repositoryUri, string localImageTag)> BuildAndPushAsync(
            CloudConnectionSecrets account, IFormFile package, string deploymentType, string appName)
        {
            var region = RegionEndpoint.GetBySystemName(account.Region);
            var ecr = new AmazonECRClient(account.AccessKey, account.SecretKey, region);
            var sts = new AmazonSecurityTokenServiceClient(account.AccessKey, account.SecretKey, region);

            var repoName = NormalizeAwsName(appName);

            string repoUri;
            try
            {
                var existing = await ecr.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                {
                    RepositoryNames = new List<string> { repoName }
                });
                repoUri = existing.Repositories[0].RepositoryUri;
            }
            catch
            {
                var created = await ecr.CreateRepositoryAsync(new CreateRepositoryRequest
                {
                    RepositoryName = repoName
                });

                repoUri = created.Repository.RepositoryUri;
            }

            var buildRoot = Path.Combine(Path.GetTempPath(), "iwx-build", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(buildRoot);

            var uploadPath = Path.Combine(buildRoot, package.FileName);
            using (var fs = File.Create(uploadPath))
            {
                await package.CopyToAsync(fs);
            }

            if (Path.GetExtension(package.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(uploadPath, buildRoot, true);
            }

            var dockerfile = Path.Combine(buildRoot, "Dockerfile");
            if (!File.Exists(dockerfile))
            {
                CreateDockerfile(buildRoot, deploymentType);
            }

            var localTag = $"iwx-{repoName}:latest";
            await EnsureDockerRunning();
            await RunProcessAsync("docker", $"build -t {localTag} \"{buildRoot}\"");

            var auth = await ecr.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());
            var token = auth.AuthorizationData[0].AuthorizationToken;
            var registry = auth.AuthorizationData[0].ProxyEndpoint;

            await RunProcessAsync("docker", $"login --username AWS --password-stdin {registry}", token);
            await RunProcessAsync("docker", $"tag {localTag} {repoUri}:latest");
            await RunProcessAsync("docker", $"push {repoUri}:latest");

            return (repoUri, localTag);
        }

        public async Task<string> CreateRepo(CloudConnectionSecrets account, string repo)
        {
            var client = new AmazonECRClient(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));

            var repos = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest());

            if(repos.Repositories.Any(x => x.RepositoryName == repo))
                return repos.Repositories.First(x => x.RepositoryName == repo).RepositoryUri;

            var result = await client.CreateRepositoryAsync(
                new CreateRepositoryRequest
                {
                    RepositoryName = repo
                }
            );

            return result.Repository.RepositoryUri;
        }
    }
}
