namespace IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs
{
    public record UpdateLogGroupRequest(
        int? RetentionInDays = null,
        string? KmsKeyId = null
    );
}
