using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudDeployments.Interfaces
{
    public interface ICloudDeploymentProvider
    {
        Task<string> Deploy(CloudConnectionSecrets account, IFormFile package, string name);

        Task Stop(CloudConnectionSecrets account, string deploymentName);

        Task Restart(CloudConnectionSecrets account, string deploymentName);
    }
}
