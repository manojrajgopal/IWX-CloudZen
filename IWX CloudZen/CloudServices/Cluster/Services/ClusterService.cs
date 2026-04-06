using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.Cluster.Providers;

namespace IWX_CloudZen.CloudServices.Cluster.Services
{
    public class ClusterService
    {
        private readonly CloudAccountService _accounts;

        public ClusterService(CloudAccountService accounts)
        {
            _accounts = accounts;
        }

        public async Task<string> SetupAwsInfrastructure(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId);

            var provider = ClusterProviderFactory.Get(account.Provider);

            var cluster = await provider.CreateCluster(account);

            return "Cluster Ready: " + cluster;
        }
    }
}
