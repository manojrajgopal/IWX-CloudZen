using IWX_CloudZen.CloudServiceCreation.Factory;
using IWX_CloudZen.CloudAccounts.Services;

namespace IWX_CloudZen.CloudServiceCreation.Services
{
    public class CloudInfrastructureService
    {
        private readonly CloudAccountService _accounts;

        public CloudInfrastructureService(CloudAccountService accounts) 
        {
            _accounts = accounts;
        }

        public async Task<string> SetupAwsInfrastructure(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId);

            var provider = ServiceFactory.Get(account.Provider);

            var cluster = await provider.CreateCluster(account);

            return "Cluster Ready: " + cluster;
        }
    }
}
