using IWX_CloudZen.CloudDeployments.Interfaces;
using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudDeployments.Providers
{
    public class AwsDeploymentProvider : ICloudDeploymentProvider
    {
        public async Task<string> Deploy(CloudConnectionSecrets account, IFormFile package, string name)
        {
            await Task.Delay(2000);
            return "Running...";
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
