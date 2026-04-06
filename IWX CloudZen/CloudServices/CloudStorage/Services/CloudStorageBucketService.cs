using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.CloudStorage.DTOs;
using IWX_CloudZen.CloudServices.CloudStorage.Providers;

namespace IWX_CloudZen.CloudServices.CloudStorage.Services
{
    public class CloudStorageBucketService
    {
        private readonly CloudAccountService _accounts;

        public CloudStorageBucketService(CloudAccountService accounts)
        {
            _accounts = accounts;
        }

        public async Task<string> CreateBucket(string user, S3BucketCreateRequest request)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, request.CloudAccountId);

            var provider = BucketProviderFactory.Get(account.Provider);

            return await provider.CreateBucket(account, request.BucketName);
        }
    }
}
