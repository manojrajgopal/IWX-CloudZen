namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public class LogGroupListResponse
    {
        public List<LogGroupResponse> LogGroups { get; set; } = new();
    }
}
