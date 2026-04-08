namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public record CreateLogGroupRequest(
        string LogGroupName,
        int? RetentionInDays = null,
        string? KmsKeyId = null,
        string? LogGroupClass = null
    );
}
