namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class CloudLogStreamInfo
    {
        public string LogStreamName { get; set; } = string.Empty;
        public string? Arn { get; set; }
        public string LogGroupName { get; set; } = string.Empty;
        public DateTime? FirstEventTimestamp { get; set; }
        public DateTime? LastEventTimestamp { get; set; }
        public DateTime? LastIngestionTime { get; set; }
        public long StoredBytes { get; set; }
    }
}
