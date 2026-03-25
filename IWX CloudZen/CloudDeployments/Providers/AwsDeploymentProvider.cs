using IWX_CloudZen.CloudDeployments.Interfaces;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudDeployments.Pipeline;

namespace IWX_CloudZen.CloudDeployments.Providers
{
    public class AwsDeploymentProvider : ICloudDeploymentProvider
    {
        private DeploymentPipeline pipeline = new DeploymentPipeline();

        public async Task<string> Deploy(CloudConnectionSecrets account, IFormFile package, string name)
        {
            var path = Path.Combine("packages", Guid.NewGuid() + ".zip");

            using var stream = new FileStream(path, FileMode.Create);

            await package.CopyToAsync(stream);

            var repo = await pipeline.RunZipDeployment(account, path, name);

            return "Deploying...";
        }

        public async Task Stop(CloudConnectionSecrets account, string deploymentName)
        {
            await Task.Delay(1000);
        }

        public async Task Restart(CloudConnectionSecrets account, string deploymentName)
        {
            await Task.Delay(1000);
        }
    }
}
