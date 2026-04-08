namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class LogGroupResponse
    {
        public int Id { get; set; }
        public string LogGroupName { get; set; } = string.Empty;
        public string? Arn { get; set; }
        public int? RetentionInDays { get; set; }
        public long StoredBytes { get; set; }
        public string MetricFilterCount { get; set; } = "0";
        public string? KmsKeyId { get; set; }
        public string? DataProtectionStatus { get; set; }
        public string? LogGroupClass { get; set; }
        public DateTime? CreationTimeUtc { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
