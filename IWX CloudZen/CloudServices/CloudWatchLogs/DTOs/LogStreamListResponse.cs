namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class LogStreamListResponse
    {
        public List<LogStreamResponse> LogStreams { get; set; } = new();
    }
}
