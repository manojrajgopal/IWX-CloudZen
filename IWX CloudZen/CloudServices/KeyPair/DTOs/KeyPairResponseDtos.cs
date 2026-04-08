namespace IWX_CloudZen.CloudServices.KeyPair.DTOs
{
    /// <summary>Standard read response for a stored key pair record.</summary>
    public class KeyPairResponse
    {
        public int Id { get; set; }
        public string KeyPairId { get; set; } = string.Empty;
        public string KeyName { get; set; } = string.Empty;
        public string KeyFingerprint { get; set; } = string.Empty;
        public string KeyType { get; set; } = string.Empty;
        public bool IsImported { get; set; }

        /// <summary>
        /// Indicates whether the private key PEM is stored in the database.
        /// Use the dedicated download endpoint to retrieve the actual PEM content.
        /// </summary>
        public bool HasPrivateKey { get; set; }

        public Dictionary<string, string> Tags { get; set; } = new();
        public DateTime? AwsCreatedAt { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response returned only when a key pair is first CREATED through this API.
    /// Contains the private key — store it immediately, it is never available again.
    /// </summary>
    public class KeyPairCreatedResponse : KeyPairResponse
    {
        /// <summary>
        /// PEM-encoded private key material.
        /// ⚠ This value is returned ONLY at creation time.
        /// It is also stored in the database for convenience, but treat it as a secret.
        /// </summary>
        public string PrivateKeyMaterial { get; set; } = string.Empty;
    }

    /// <summary>Response returned when downloading the private key PEM from DB.</summary>
    public class KeyPairPrivateKeyResponse
    {
        public int Id { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string PrivateKeyMaterial { get; set; } = string.Empty;
    }

    public class KeyPairListResponse
    {
        public List<KeyPairResponse> KeyPairs { get; set; } = new();
    }

    public class SyncKeyPairsResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<KeyPairResponse> KeyPairs { get; set; } = new();
    }
}
