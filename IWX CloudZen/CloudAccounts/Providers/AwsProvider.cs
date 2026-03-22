using Amazon;
using Amazon.S3;
using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.CloudAccounts.Entities;

namespace IWX_CloudZen.CloudAccounts.Providers
{
    public class AwsProvider : ICloudProvider
    {
        public async Task<bool> ValidateConnection(CloudAccount account)
        {
            try
            {
                var client = new AmazonS3Client(
                    account.AccessKey,
                    account.SecretKey,
                    RegionEndpoint.GetBySystemName(account.Region)
                );

                var buckets = await client.ListBucketsAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetStorageList(CloudAccount account)
        {
            var client = new AmazonS3Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region)
            );

            var result = await client.ListBucketsAsync();

            return result.Buckets.Select(x => x.BucketName).ToList();
        }
    }
}
