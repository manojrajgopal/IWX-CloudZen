using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServiceCreation.DTOs;

namespace IWX_CloudZen.CloudServiceCreation.Interfaces
{
    public interface ICloudServiceCreator
    {
        Task<string> CreateCluster(CloudConnectionSecrets account);

        Task<string> CreateTaskDefinition(CloudConnectionSecrets account, string image);

        Task<string> CreateService(CloudConnectionSecrets account, string cluster, string taskDefinition);
        
        Task<AwsInfrastructureResult> EnsureInfrastructureAsync(CloudConnectionSecrets account, string appName);
    }
}
