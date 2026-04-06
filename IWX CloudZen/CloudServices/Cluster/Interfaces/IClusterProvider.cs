using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.Cluster.DTOs;

namespace IWX_CloudZen.CloudServices.Cluster.Interfaces
{
    public interface IClusterProvider
    {
        Task<ClusterListResponse> ListClusters(CloudConnectionSecrets account);

        Task<ClusterResponse> CreateCluster(CloudConnectionSecrets account, string clusterName);

        Task<string> CreateTaskDefinition(CloudConnectionSecrets account, string image);

        Task<string> CreateService(CloudConnectionSecrets account, string cluster, string taskDefinition);
    }
}
