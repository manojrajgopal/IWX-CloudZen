namespace IWX_CloudZen.CloudServices.KeyPair.DTOs
{
    /// <summary>Raw key pair data as fetched from the cloud provider.</summary>
    public class CloudKeyPairInfo
    {
        public string KeyPairId { get; set; } = string.Empty;
        public string KeyName { get; set; } = string.Empty;
        public string KeyFingerprint { get; set; } = string.Empty;
        public string KeyType { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new();
        public DateTime? AwsCreatedAt { get; set; }
    }

    /// <summary>
    /// Extended cloud info returned only at CREATE time — contains the private key
    /// which AWS provides exactly once and never again.
    /// </summary>
    public class CloudKeyPairCreatedInfo : CloudKeyPairInfo
    {
        /// <summary>PEM-encoded private key. Available only at creation time.</summary>
        public string PrivateKeyMaterial { get; set; } = string.Empty;
    }
}
