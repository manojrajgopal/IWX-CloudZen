namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class CloudLogGroupInfo
    {
        public string LogGroupName { get; set; } = string.Empty;
        public string? Arn { get; set; }
        public int? RetentionInDays { get; set; }
        public long StoredBytes { get; set; }
        public int MetricFilterCount { get; set; }
        public string? KmsKeyId { get; set; }
        public string? DataProtectionStatus { get; set; }
        public string? LogGroupClass { get; set; }
        public DateTime? CreationTimeUtc { get; set; }
    }
}
