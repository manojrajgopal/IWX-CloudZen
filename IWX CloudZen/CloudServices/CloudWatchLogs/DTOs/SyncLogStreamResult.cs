namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class SyncLogStreamResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<LogStreamResponse> LogStreams { get; set; } = new();
    }
}
