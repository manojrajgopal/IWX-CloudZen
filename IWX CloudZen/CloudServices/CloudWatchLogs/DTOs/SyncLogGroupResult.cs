namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class SyncLogGroupResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<LogGroupResponse> LogGroups { get; set; } = new();
    }
}
