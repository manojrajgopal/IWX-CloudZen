namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public record PutLogEventsRequest(
        string LogStreamName,
        List<LogEventItem> LogEvents
    );

    public record LogEventItem(
        string Message,
        DateTime? Timestamp = null
    );
}
