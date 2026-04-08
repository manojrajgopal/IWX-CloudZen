using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs;

namespace IWX_CloudZen.CloudServices.CloudWatchLogs.Interfaces
{
    public interface ICloudWatchLogsProvider
    {
        // Log Groups
        Task<List<CloudLogGroupInfo>> FetchAllLogGroups(CloudConnectionSecrets account);

        Task<CloudLogGroupInfo> CreateLogGroup(CloudConnectionSecrets account, string logGroupName, int? retentionInDays, string? kmsKeyId, string? logGroupClass);

        Task<CloudLogGroupInfo> UpdateLogGroup(CloudConnectionSecrets account, string logGroupName, int? retentionInDays, string? kmsKeyId);

        Task DeleteLogGroup(CloudConnectionSecrets account, string logGroupName);

        // Log Streams
        Task<List<CloudLogStreamInfo>> FetchLogStreams(CloudConnectionSecrets account, string logGroupName);

        Task<CloudLogStreamInfo> CreateLogStream(CloudConnectionSecrets account, string logGroupName, string logStreamName);

        Task DeleteLogStream(CloudConnectionSecrets account, string logGroupName, string logStreamName);

        // Log Events
        Task<LogEventsListResponse> GetLogEvents(CloudConnectionSecrets account, string logGroupName, string logStreamName, int limit, string? nextToken);

        Task PutLogEvents(CloudConnectionSecrets account, string logGroupName, string logStreamName, List<LogEventItem> events);

        Task<LogEventsListResponse> FilterLogEvents(CloudConnectionSecrets account, string logGroupName, FilterLogEventsRequest filter);
    }
}
