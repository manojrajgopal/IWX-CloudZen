namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class LogEventResponse
    {
        public string LogStreamName { get; set; } = string.Empty;
        public string LogGroupName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? IngestionTime { get; set; }
    }

    public class LogEventsListResponse
    {
        public string LogGroupName { get; set; } = string.Empty;
        public string LogStreamName { get; set; } = string.Empty;
        public List<LogEventResponse> Events { get; set; } = new();
        public string? NextForwardToken { get; set; }
        public string? NextBackwardToken { get; set; }
    }
}
