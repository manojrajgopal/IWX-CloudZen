using Docker.DotNet.Models;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudDeployments.DTOs;
using IWX_CloudZen.CloudDeployments.Interfaces;

namespace IWX_CloudZen.CloudDeployments.Providers
{
    public class AzureDeploymentProvider : ICloudDeploymentProvider
    {
        public async Task<AwsDeploymentResult> Deploy(CloudConnectionSecrets account, IFormFile package, string name, string deploymentType)
        {
            await Task.Delay(2000);
            return new AwsDeploymentResult
            {
                Status = "Running",
                ImageUrl = "sample image",
                ServiceName = name,
                ClusterName = "sample infra.ClusterName",
                HealthUrl = $"http://...",
                LogsGroup = "infra.LogGroupName"
            };
        }

        public async Task Stop(CloudConnectionSecrets account, string deploymentName)
        {
            Task.Delay(1000);
        }

        public async Task Restart(CloudConnectionSecrets account, string deploymentName)
        {
            await Task.Delay(1000);
        }
    }
}
