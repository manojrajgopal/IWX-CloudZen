namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public record FilterLogEventsRequest(
        string? LogStreamName = null,
        string? FilterPattern = null,
        DateTime? StartTime = null,
        DateTime? EndTime = null,
        int Limit = 100
    );
}
