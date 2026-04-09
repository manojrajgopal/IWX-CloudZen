using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.KeyPair.DTOs;
using IWX_CloudZen.CloudServices.KeyPair.Interfaces;

// Disambiguate DTO names that clash with Amazon.EC2.Model types
using AwsCreateKeyPairRequest = Amazon.EC2.Model.CreateKeyPairRequest;
using AwsImportKeyPairRequest = Amazon.EC2.Model.ImportKeyPairRequest;

namespace IWX_CloudZen.CloudServices.KeyPair.Providers
{
    public class AwsKeyPairProvider : IKeyPairProvider
    {
        // ---- Client ----

        private static AmazonEC2Client GetClient(CloudConnectionSecrets account) =>
            new AmazonEC2Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));

        // ---- Helpers / Mappers ----

        private static Dictionary<string, string> MapTags(List<Tag>? tags) =>
            tags?.ToDictionary(t => t.Key, t => t.Value) ?? new();

        private static List<Tag> BuildAwsTags(Dictionary<string, string>? tags) =>
            tags is { Count: > 0 }
                ? tags.Select(kvp => new Tag { Key = kvp.Key, Value = kvp.Value }).ToList()
                : new();

        private static CloudKeyPairInfo MapInfo(KeyPairInfo kp) => new()
        {
            KeyPairId          = kp.KeyPairId ?? string.Empty,
            KeyName            = kp.KeyName,
            KeyFingerprint     = kp.KeyFingerprint ?? string.Empty,
            KeyType            = kp.KeyType?.Value ?? string.Empty,
            PublicKeyMaterial  = kp.PublicKey ?? string.Empty,
            Tags               = MapTags(kp.Tags),
            AwsCreatedAt       = kp.CreateTime
        };

        // ---- Interface Implementation ----

        public async Task<List<CloudKeyPairInfo>> FetchAllKeyPairs(CloudConnectionSecrets account)
        {
            var client = GetClient(account);

            var response = await client.DescribeKeyPairsAsync(new DescribeKeyPairsRequest
            {
                IncludePublicKey = true
            });

            return response.KeyPairs.Select(MapInfo).ToList();
        }

        public async Task<CloudKeyPairInfo> GetKeyPair(CloudConnectionSecrets account, string keyName)
        {
            var client = GetClient(account);

            var response = await client.DescribeKeyPairsAsync(new DescribeKeyPairsRequest
            {
                KeyNames        = [keyName],
                IncludePublicKey = true
            });

            var kp = response.KeyPairs.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Key pair '{keyName}' not found.");

            return MapInfo(kp);
        }

        public async Task<CloudKeyPairCreatedInfo> CreateKeyPair(
            CloudConnectionSecrets account,
            string keyName,
            string keyType,
            Dictionary<string, string>? tags)
        {
            var client = GetClient(account);

            var request = new AwsCreateKeyPairRequest
            {
                KeyName = keyName,
                KeyType = KeyType.FindValue(keyType) ?? KeyType.Rsa
            };

            if (tags is { Count: > 0 })
            {
                request.TagSpecifications =
                [
                    new TagSpecification
                    {
                        ResourceType = ResourceType.KeyPair,
                        Tags = BuildAwsTags(tags)
                    }
                ];
            }

            var response = await client.CreateKeyPairAsync(request);

            // Fetch the public key material (not included in CreateKeyPair response)
            var describeResponse = await client.DescribeKeyPairsAsync(new DescribeKeyPairsRequest
            {
                KeyPairIds      = [response.KeyPair.KeyPairId],
                IncludePublicKey = true
            });

            var publicKey = describeResponse.KeyPairs.FirstOrDefault()?.PublicKey ?? string.Empty;

            return new CloudKeyPairCreatedInfo
            {
                KeyPairId          = response.KeyPair.KeyPairId ?? string.Empty,
                KeyName            = response.KeyPair.KeyName,
                KeyFingerprint     = response.KeyPair.KeyFingerprint ?? string.Empty,
                KeyType            = keyType,
                PublicKeyMaterial  = publicKey,
                Tags               = tags ?? new(),
                AwsCreatedAt       = DateTime.UtcNow,
                PrivateKeyMaterial = response.KeyPair.KeyMaterial ?? string.Empty
            };
        }

        public async Task<CloudKeyPairInfo> ImportKeyPair(
            CloudConnectionSecrets account,
            string keyName,
            string publicKeyMaterial,
            Dictionary<string, string>? tags)
        {
            var client = GetClient(account);

            var request = new AwsImportKeyPairRequest
            {
                KeyName           = keyName,
                PublicKeyMaterial = publicKeyMaterial
            };

            if (tags is { Count: > 0 })
            {
                request.TagSpecifications =
                [
                    new TagSpecification
                    {
                        ResourceType = ResourceType.KeyPair,
                        Tags = BuildAwsTags(tags)
                    }
                ];
            }

            var response = await client.ImportKeyPairAsync(request);

            return new CloudKeyPairInfo
            {
                KeyPairId      = response.KeyPairId ?? string.Empty,
                KeyName        = response.KeyName,
                KeyFingerprint = response.KeyFingerprint ?? string.Empty,
                KeyType        = "rsa",  // AWS only accepts RSA for imported keys
                Tags           = tags ?? new(),
                AwsCreatedAt   = DateTime.UtcNow
            };
        }

        public async Task UpdateKeyPairTags(
            CloudConnectionSecrets account,
            string keyName,
            Dictionary<string, string> tags)
        {
            var client = GetClient(account);

            // Resolve the actual KeyPairId from the name (tags API requires IDs for key pairs)
            var describeResponse = await client.DescribeKeyPairsAsync(new DescribeKeyPairsRequest
            {
                KeyNames = [keyName]
            });

            var kp = describeResponse.KeyPairs.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Key pair '{keyName}' not found.");

            await client.CreateTagsAsync(new CreateTagsRequest
            {
                Resources = [kp.KeyPairId],
                Tags      = BuildAwsTags(tags)
            });
        }

        public async Task DeleteKeyPair(CloudConnectionSecrets account, string keyName)
        {
            var client = GetClient(account);

            await client.DeleteKeyPairAsync(new DeleteKeyPairRequest
            {
                KeyName = keyName
            });
        }
    }
}
