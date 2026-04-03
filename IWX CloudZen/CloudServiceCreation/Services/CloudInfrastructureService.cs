using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServiceCreation.Factory;
using IWX_CloudZen.CloudServiceCreation.Providers;

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
            var account = await _accounts.ResolveCredentialsAsync(user, accountId) ?? throw new Exception("Cloud account not found.");

            var creator = new AwsServiceCreator();
            var infra = await creator.EnsureInfrastructureAsync(account, account.AccountName);

            return $"AWS infrastructure ready. VPC={infra.VpcId}, Cluster={infra.ClusterName}, ALB={infra.LoadBalancerDnsName}";
        }
    }
}
