using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServiceCreation.Interfaces;

namespace IWX_CloudZen.CloudServiceCreation.Providers
{
    public class AwsS3ServiceCreator : IStorageInfrastructure
    {
        private AmazonS3Client GetClient(CloudConnectionSecrets account)
        {
            return new AmazonS3Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region)
            );
        }

        public async Task<string> CreateBucket( CloudConnectionSecrets account, string bucketName)
        {
            var client = GetClient(account);

            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName);

            if (exists)
                return "Bucket already exists";

            var request = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            };

            await client.PutBucketAsync(request);

            return "Bucket Created : " + bucketName;
        }
    }
}