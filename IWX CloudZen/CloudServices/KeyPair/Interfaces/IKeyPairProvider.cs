using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.KeyPair.DTOs;

namespace IWX_CloudZen.CloudServices.KeyPair.Interfaces
{
    public interface IKeyPairProvider
    {
        /// <summary>Fetch all key pairs visible to this account in the region.</summary>
        Task<List<CloudKeyPairInfo>> FetchAllKeyPairs(CloudConnectionSecrets account);

        /// <summary>Fetch a single key pair by name.</summary>
        Task<CloudKeyPairInfo> GetKeyPair(CloudConnectionSecrets account, string keyName);

        /// <summary>
        /// Create a new key pair in AWS. Returns a <see cref="CloudKeyPairCreatedInfo"/>
        /// which includes the private key PEM — available only at this call.
        /// </summary>
        Task<CloudKeyPairCreatedInfo> CreateKeyPair(
            CloudConnectionSecrets account,
            string keyName,
            string keyType,
            Dictionary<string, string>? tags);

        /// <summary>
        /// Import an existing public key into AWS.
        /// Returns standard info (no private key — we do not have it).
        /// </summary>
        Task<CloudKeyPairInfo> ImportKeyPair(
            CloudConnectionSecrets account,
            string keyName,
            string publicKeyMaterial,
            Dictionary<string, string>? tags);

        /// <summary>Update tags on a key pair (AWS does not allow renaming key pairs).</summary>
        Task UpdateKeyPairTags(
            CloudConnectionSecrets account,
            string keyName,
            Dictionary<string, string> tags);

        /// <summary>Permanently delete a key pair from AWS.</summary>
        Task DeleteKeyPair(CloudConnectionSecrets account, string keyName);
    }
}
