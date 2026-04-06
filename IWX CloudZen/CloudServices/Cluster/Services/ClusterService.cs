using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.Cluster.DTOs;
using IWX_CloudZen.CloudServices.Cluster.Factory;

namespace IWX_CloudZen.CloudServices.Cluster.Services
{
    public class ClusterService
    {
        private readonly CloudAccountService _accounts;

        public ClusterService(CloudAccountService accounts)
        {
            _accounts = accounts;
        }

        public async Task<ClusterListResponse> ListAwsClusters(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = ClusterProviderFactory.Get(account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            return await provider.ListClusters(account);
        }

        public async Task<ClusterResponse> SetupAwsInfrastructure(string user, int accountId, string clusterName)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = ClusterProviderFactory.Get(account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            return await provider.CreateCluster(account, clusterName);
        }
    }
}
