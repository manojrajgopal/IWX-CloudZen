namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    /// <summary>Raw ECR image data fetched from AWS, used for sync.</summary>
    public class CloudImageInfo
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string? ImageTag { get; set; }
        public string ImageDigest { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public string? ScanStatus { get; set; }
        public int FindingsCritical { get; set; }
        public int FindingsHigh { get; set; }
        public int FindingsMedium { get; set; }
        public int FindingsLow { get; set; }
        public DateTime? PushedAt { get; set; }
    }
}
