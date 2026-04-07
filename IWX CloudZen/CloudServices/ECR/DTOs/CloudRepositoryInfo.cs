namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    /// <summary>Raw ECR repository data fetched from AWS, used for sync.</summary>
    public class CloudRepositoryInfo
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string RepositoryArn { get; set; } = string.Empty;
        public string RepositoryUri { get; set; } = string.Empty;
        public string ImageTagMutability { get; set; } = string.Empty;
        public bool ScanOnPush { get; set; }
        public string EncryptionType { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
