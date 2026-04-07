namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class ImageFindingSummary
    {
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
    }

    public class ImageResponse
    {
        public int Id { get; set; }
        public int RepositoryRecordId { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public string? ImageTag { get; set; }
        public string? ImageDigest { get; set; }
        public long SizeInBytes { get; set; }
        public string SizeFormatted => SizeInBytes > 0
            ? $"{Math.Round(SizeInBytes / 1_048_576.0, 2)} MB"
            : "0 MB";
        public string? ScanStatus { get; set; }
        public ImageFindingSummary? Findings { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime? PushedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
