using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudDeployments.DTOs;

namespace IWX_CloudZen.CloudDeployments.Interfaces
{
    public interface ICloudDeploymentProvider
    {
        Task<AwsDeploymentResult> Deploy(CloudConnectionSecrets account, IFormFile package, string name, string deploymentType);

        Task Stop(CloudConnectionSecrets account, string deploymentName);

        Task Restart(CloudConnectionSecrets account, string deploymentName);
    }
}
