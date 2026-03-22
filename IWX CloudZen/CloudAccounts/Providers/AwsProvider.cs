using Amazon;
using Amazon.S3;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Interfaces;

namespace IWX_CloudZen.CloudAccounts.Providers
{
    public class AwsProvider : ICloudProvider
    {
        public async Task<bool> ValidateConnectionAsync(ConnectCloudRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AccessKey) ||
                string.IsNullOrWhiteSpace(request.SecretKey) ||
                string.IsNullOrWhiteSpace(request.Region))
            {
                return false;
            }

            try
            {
                var client = new AmazonS3Client(
                    request.AccessKey,
                    request.SecretKey,
                    RegionEndpoint.GetBySystemName(request.Region));

                await client.ListBucketsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}