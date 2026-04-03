using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon;

namespace IWX_CloudZen.CloudDeployments.Logs
{
    public class CloudWatchService
    {
        public async Task<List<string>> GetLogs(string group, string stream, string key, string secret, string region)
        {
            var client = new AmazonCloudWatchLogsClient(key, secret, RegionEndpoint.GetBySystemName(region));

            var logs = await client.GetLogEventsAsync(new GetLogEventsRequest { LogGroupName = group, LogStreamName = stream });

            return logs.Events.Select(x => x.Message).ToList();
        }
    }
}
