using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudServices.Cluster.Interfaces
{
    public interface IClusterProvider
    {
        Task<string> CreateCluster(CloudConnectionSecrets account);

        Task<string> CreateTaskDefinition(CloudConnectionSecrets account, string image);

        Task<string> CreateService(CloudConnectionSecrets account, string cluster, string taskDefinition);
    }
}
