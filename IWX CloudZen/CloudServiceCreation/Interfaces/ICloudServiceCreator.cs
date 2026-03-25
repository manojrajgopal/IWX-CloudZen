using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudServiceCreation.Interfaces
{
    public interface ICloudServiceCreator
    {
        Task<string> CreateCluster(CloudConnectionSecrets account);

        Task<string> CreateTaskDefinition(CloudConnectionSecrets account, string image);

        Task<string> CreateService(CloudConnectionSecrets account, string cluster, string taskDefinition);
    }
}
