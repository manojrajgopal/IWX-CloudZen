using IWX_CloudZen.CloudStorage.Interfaces;
using IWX_CloudZen.CloudAccounts.DTOs;
using Amazon.S3;
using Amazon;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using System.Net.Mime;
using Amazon.S3.Util;

namespace IWX_CloudZen.CloudStorage.Providers
{
    public class AwsStorageProvider : IFileStorageProvider
    {
        public const string bucket = "cloudzen-storage-iwx-001";
        public async Task<string> UploadFile(CloudConnectionSecrets account, IFormFile file, string folder)
        {
            try
            {
                var client = new AmazonS3Client(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));

                await EnsureBucket(client, account.Region);

                var key = folder + "/" + Guid.NewGuid() + "_" + file.FileName;

                using var stream = file.OpenReadStream();

                var request = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = key,
                    BucketName = bucket,
                    ContentType = file.ContentType
                };

                var transfer = new TransferUtility(client);

                await transfer.UploadAsync(request);

                return key;
            }
            catch (AmazonS3Exception ex)
            {
                throw new Exception("AWS upload failed: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Upload failed: " + ex.Message);
            }
        }

        private async Task EnsureBucket(AmazonS3Client client, string region)
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket);

            if (exists)
                return;

            var request = new PutBucketRequest { BucketName = bucket, BucketRegion = S3Region.USEast1 };

            await client.PutBucketAsync(request);
        }

        public async Task DeleteFile(CloudConnectionSecrets account, string fileUrl)
        {
            try
            {
                var client = new AmazonS3Client(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));

                await client.DeleteObjectAsync(bucket, fileUrl);
            }
            catch (Exception ex)
            {
                throw new Exception("Delete failed: " + ex.Message);
            }
        }

        public async Task<Stream> DownloadFile(CloudConnectionSecrets account, string fileUrl)
        {
            try
            {
                var client = new AmazonS3Client(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));

                var response = await client.GetObjectAsync(bucket, fileUrl);

                var memory = new MemoryStream();

                await response.ResponseStream.CopyToAsync(memory);

                memory.Position = 0;

                return memory;
            }
            catch (Exception ex)
            {
                throw new Exception("Download failed: " + ex.Message);
            }
        }
    }
}
