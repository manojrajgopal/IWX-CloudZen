using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.CloudStorage.DTOs;
using IWX_CloudZen.CloudServices.CloudStorage.Interfaces;

namespace IWX_CloudZen.CloudServices.CloudStorage.Providers
{
    public class AwsS3Provider : IStorageProvider
    {
        private AmazonS3Client GetClient(CloudConnectionSecrets account)
            => new(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));

        // ---- Buckets ----

        public async Task<CloudBucketInfo> CreateBucket(CloudConnectionSecrets account, string bucketName)
        {
            var client = GetClient(account);

            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName);

            if (!exists)
            {
                await client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                });
            }

            return new CloudBucketInfo
            {
                Name = bucketName,
                Region = account.Region ?? "us-east-1",
                Status = exists ? "Already Exists" : "Created"
            };
        }

        public async Task DeleteBucket(CloudConnectionSecrets account, string bucketName)
        {
            var client = GetClient(account);

            // Delete all objects before deleting the bucket
            string? continuationToken = null;
            do
            {
                var listResponse = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    ContinuationToken = continuationToken
                });

                if (listResponse.S3Objects?.Count > 0)
                {
                    await client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = listResponse.S3Objects
                            .Select(o => new KeyVersion { Key = o.Key })
                            .ToList()
                    });
                }

                continuationToken = listResponse.IsTruncated == true ? listResponse.NextContinuationToken : null;
            }
            while (continuationToken != null);

            await client.DeleteBucketAsync(new DeleteBucketRequest { BucketName = bucketName });
        }

        public async Task<List<CloudBucketInfo>> FetchAllBuckets(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var response = await client.ListBucketsAsync();
            var result = new List<CloudBucketInfo>();

            foreach (var bucket in response.Buckets)
            {
                string region;
                try
                {
                    var loc = await client.GetBucketLocationAsync(bucket.BucketName);
                    region = string.IsNullOrEmpty(loc.Location?.Value) ? "us-east-1" : loc.Location.Value;
                }
                catch
                {
                    region = account.Region ?? "us-east-1";
                }

                result.Add(new CloudBucketInfo
                {
                    Name = bucket.BucketName,
                    Region = region,
                    Status = "Active"
                });
            }

            return result;
        }

        // ---- Files ----

        public async Task<CloudFileInfo> UploadFile(CloudConnectionSecrets account, string bucketName, IFormFile file, string folder)
        {
            var client = GetClient(account);

            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName);
            if (!exists)
                throw new InvalidOperationException($"Bucket '{bucketName}' does not exist.");

            var key = string.IsNullOrWhiteSpace(folder)
                ? $"{Guid.NewGuid()}_{file.FileName}"
                : $"{folder.TrimEnd('/')}/{Guid.NewGuid()}_{file.FileName}";

            using var stream = file.OpenReadStream();

            await new TransferUtility(client).UploadAsync(new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = bucketName,
                ContentType = file.ContentType
            });

            var metadata = await client.GetObjectMetadataAsync(bucketName, key);

            return new CloudFileInfo
            {
                Key = key,
                FileName = file.FileName,
                ETag = metadata.ETag.Trim('"'),
                Size = file.Length,
                LastModified = DateTime.UtcNow,
                ContentType = file.ContentType
            };
        }

        public async Task<Stream> DownloadFile(CloudConnectionSecrets account, string bucketName, string fileKey)
        {
            var client = GetClient(account);
            var response = await client.GetObjectAsync(bucketName, fileKey);

            var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory);
            memory.Position = 0;

            return memory;
        }

        public async Task DeleteFile(CloudConnectionSecrets account, string bucketName, string fileKey)
        {
            var client = GetClient(account);
            await client.DeleteObjectAsync(bucketName, fileKey);
        }

        public async Task<CloudFileInfo> ReplaceFile(CloudConnectionSecrets account, string bucketName, string fileKey, IFormFile newFile)
        {
            var client = GetClient(account);

            using var stream = newFile.OpenReadStream();

            await new TransferUtility(client).UploadAsync(new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = fileKey,
                BucketName = bucketName,
                ContentType = newFile.ContentType
            });

            var metadata = await client.GetObjectMetadataAsync(bucketName, fileKey);

            return new CloudFileInfo
            {
                Key = fileKey,
                FileName = newFile.FileName,
                ETag = metadata.ETag.Trim('"'),
                Size = newFile.Length,
                LastModified = DateTime.UtcNow,
                ContentType = newFile.ContentType
            };
        }

        public async Task<List<CloudFileInfo>> FetchAllFiles(CloudConnectionSecrets account, string bucketName)
        {
            // Resolve the bucket's actual region so we connect to the correct regional endpoint.
            // This is necessary for opt-in regions (e.g. ap-south-2) where a client configured for
            // a different region will receive a "location constraint is incompatible" error.
            var baseClient = GetClient(account);
            AmazonS3Client client;
            try
            {
                var loc = await baseClient.GetBucketLocationAsync(bucketName);
                var bucketRegion = string.IsNullOrEmpty(loc.Location?.Value) ? "us-east-1" : loc.Location.Value;
                client = bucketRegion != account.Region
                    ? new AmazonS3Client(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(bucketRegion))
                    : baseClient;
            }
            catch
            {
                client = baseClient;
            }

            var result = new List<CloudFileInfo>();
            string? continuationToken = null;

            do
            {
                var response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    ContinuationToken = continuationToken
                });

                foreach (var obj in response.S3Objects ?? Enumerable.Empty<S3Object>())
                {
                    result.Add(new CloudFileInfo
                    {
                        Key = obj.Key,
                        FileName = Path.GetFileName(obj.Key),
                        ETag = obj.ETag?.Trim('"') ?? string.Empty,
                        Size = obj.Size ?? 0,
                        LastModified = obj.LastModified ?? DateTime.UtcNow,
                        ContentType = string.Empty
                    });
                }

                continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
            }
            while (continuationToken != null);

            return result;
        }
    }
}
