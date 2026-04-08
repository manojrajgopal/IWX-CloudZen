using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.KeyPair.Entities
{
    public class KeyPairRecord
    {
        public int Id { get; set; }

        /// <summary>AWS Key Pair ID, e.g. key-0abc1234567890abc.</summary>
        [Required, MaxLength(100)]
        public string KeyPairId { get; set; } = string.Empty;

        /// <summary>The name of the key pair as set on the cloud provider.</summary>
        [Required, MaxLength(256)]
        public string KeyName { get; set; } = string.Empty;

        /// <summary>
        /// SHA-1 fingerprint for RSA keys (created via AWS).
        /// MD5 fingerprint for keys created outside AWS and imported.
        /// </summary>
        [MaxLength(256)]
        public string KeyFingerprint { get; set; } = string.Empty;

        /// <summary>Key algorithm: rsa or ed25519.</summary>
        [MaxLength(20)]
        public string KeyType { get; set; } = string.Empty;

        /// <summary>
        /// PEM-encoded private key material returned by AWS at creation time only.
        /// NULL for keys that were imported or discovered via sync.
        /// </summary>
        public string? PrivateKeyMaterial { get; set; }

        /// <summary>
        /// PEM-encoded public key material.
        /// Populated for imported keys; may be null for keys created by AWS.
        /// </summary>
        public string? PublicKeyMaterial { get; set; }

        /// <summary>Indicates whether this key pair was imported from an external public key.</summary>
        public bool IsImported { get; set; }

        /// <summary>JSON object of tags attached to this key pair.</summary>
        public string? TagsJson { get; set; }

        /// <summary>Timestamp when AWS created this key pair.</summary>
        public DateTime? AwsCreatedAt { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
