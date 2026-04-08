namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class LogStreamResponse
    {
        public int Id { get; set; }
        public string LogStreamName { get; set; } = string.Empty;
        public string? Arn { get; set; }
        public string LogGroupName { get; set; } = string.Empty;
        public int LogGroupRecordId { get; set; }
        public DateTime? FirstEventTimestamp { get; set; }
        public DateTime? LastEventTimestamp { get; set; }
        public DateTime? LastIngestionTime { get; set; }
        public long StoredBytes { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
