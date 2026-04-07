namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class RepositoryResponse
    {
        public int Id { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public string? RepositoryArn { get; set; }
        public string? RepositoryUri { get; set; }
        public string ImageTagMutability { get; set; } = string.Empty;
        public bool ScanOnPush { get; set; }
        public string EncryptionType { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
