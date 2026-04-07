using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.ECR.DTOs;
using IWX_CloudZen.CloudServices.ECR.Interfaces;

namespace IWX_CloudZen.CloudServices.ECR.Providers
{
    public class AwsEcrProvider : IEcrProvider
    {
        // ---- Client ----

        private static AmazonECRClient GetClient(CloudConnectionSecrets account)
        {
            return new AmazonECRClient(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));
        }

        // ---- Helpers ----

        private static CloudRepositoryInfo MapRepository(Repository r) => new()
        {
            RepositoryName = r.RepositoryName,
            RepositoryArn = r.RepositoryArn,
            RepositoryUri = r.RepositoryUri,
            ImageTagMutability = r.ImageTagMutability?.Value ?? "MUTABLE",
            ScanOnPush = r.ImageScanningConfiguration?.ScanOnPush == true,
            EncryptionType = r.EncryptionConfiguration?.EncryptionType?.Value ?? "AES256",
            CreatedAt = r.CreatedAt
        };

        // ---- IEcrProvider: Repositories ----

        public async Task<List<CloudRepositoryInfo>> FetchAllRepositories(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var result = new List<CloudRepositoryInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                {
                    NextToken = nextToken
                });

                foreach (var repo in response.Repositories ?? [])
                    result.Add(MapRepository(repo));

                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
        }

        public async Task<CloudRepositoryInfo> CreateRepository(
            CloudConnectionSecrets account,
            string repositoryName,
            string imageTagMutability,
            bool scanOnPush,
            string encryptionType)
        {
            var client = GetClient(account);

            var response = await client.CreateRepositoryAsync(new Amazon.ECR.Model.CreateRepositoryRequest
            {
                RepositoryName = repositoryName,
                ImageTagMutability = imageTagMutability,
                ImageScanningConfiguration = new ImageScanningConfiguration
                {
                    ScanOnPush = scanOnPush
                },
                EncryptionConfiguration = new EncryptionConfiguration
                {
                    EncryptionType = encryptionType
                }
            });

            return MapRepository(response.Repository);
        }

        public async Task<CloudRepositoryInfo> UpdateRepository(
            CloudConnectionSecrets account,
            string repositoryName,
            string? imageTagMutability,
            bool? scanOnPush)
        {
            var client = GetClient(account);

            if (!string.IsNullOrWhiteSpace(imageTagMutability))
            {
                await client.PutImageTagMutabilityAsync(new PutImageTagMutabilityRequest
                {
                    RepositoryName = repositoryName,
                    ImageTagMutability = imageTagMutability
                });
            }

            if (scanOnPush.HasValue)
            {
                await client.PutImageScanningConfigurationAsync(new PutImageScanningConfigurationRequest
                {
                    RepositoryName = repositoryName,
                    ImageScanningConfiguration = new ImageScanningConfiguration
                    {
                        ScanOnPush = scanOnPush.Value
                    }
                });
            }

            // Re-describe to return current state
            var describeResponse = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
            {
                RepositoryNames = [repositoryName]
            });

            var repo = describeResponse.Repositories?.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Repository '{repositoryName}' not found in AWS.");

            return MapRepository(repo);
        }

        public async Task DeleteRepository(
            CloudConnectionSecrets account, string repositoryName, bool force)
        {
            var client = GetClient(account);

            await client.DeleteRepositoryAsync(new DeleteRepositoryRequest
            {
                RepositoryName = repositoryName,
                Force = force
            });
        }

        // ---- IEcrProvider: Images ----

        public async Task<List<CloudImageInfo>> FetchAllImages(
            CloudConnectionSecrets account, string repositoryName)
        {
            var client = GetClient(account);
            var imageDetails = new List<ImageDetail>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeImagesAsync(new DescribeImagesRequest
                {
                    RepositoryName = repositoryName,
                    NextToken = nextToken
                });

                imageDetails.AddRange(response.ImageDetails ?? []);
                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return imageDetails.Select(img =>
            {
                var findings = img.ImageScanFindingsSummary?.FindingSeverityCounts;
                return new CloudImageInfo
                {
                    RepositoryName = repositoryName,
                    ImageTag = img.ImageTags?.FirstOrDefault(),
                    ImageDigest = img.ImageDigest ?? string.Empty,
                    SizeInBytes = img.ImageSizeInBytes ?? 0,
                    ScanStatus = img.ImageScanStatus?.Status?.Value,
                    FindingsCritical = findings != null && findings.TryGetValue("CRITICAL", out var c) ? c : 0,
                    FindingsHigh = findings != null && findings.TryGetValue("HIGH", out var h) ? h : 0,
                    FindingsMedium = findings != null && findings.TryGetValue("MEDIUM", out var m) ? m : 0,
                    FindingsLow = findings != null && findings.TryGetValue("LOW", out var l) ? l : 0,
                    PushedAt = img.ImagePushedAt
                };
            }).ToList();
        }

        public async Task DeleteImage(
            CloudConnectionSecrets account, string repositoryName, string imageDigest)
        {
            var client = GetClient(account);

            await client.BatchDeleteImageAsync(new BatchDeleteImageRequest
            {
                RepositoryName = repositoryName,
                ImageIds = [new ImageIdentifier { ImageDigest = imageDigest }]
            });
        }
    }
}
