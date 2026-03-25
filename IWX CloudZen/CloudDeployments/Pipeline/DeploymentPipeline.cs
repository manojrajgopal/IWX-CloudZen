using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudDeployments.DTOs;

namespace IWX_CloudZen.CloudDeployments.Pipeline
{
    public class DeploymentPipeline
    {
        private readonly DockerBuilder _docker = new();
        private readonly EcrPushService _ecr = new();

        public async Task<string> RunZipDeployment(CloudConnectionSecrets account, string zipPath, string name)
        {
            var extract = Path.Combine("deployments", Guid.NewGuid().ToString());

            Directory.CreateDirectory(extract);

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extract);

            var image = $"iwx/{name.ToLower()}";

            await _docker.Build(extract, image);

            var repo = await _ecr.CreateRepo(account, name);

            return repo;
        }
    }
}
