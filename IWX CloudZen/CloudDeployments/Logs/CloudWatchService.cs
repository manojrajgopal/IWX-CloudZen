using Amazon.CloudWatchLogs;
using Amazon;

namespace IWX_CloudZen.CloudDeployments.Logs
{
    public class CloudWatchService
    {
        public async Task<List<string>> GetLogs()
        {
            return new List<string>
            {
                "Container started",
                "App running"
            };
        }
    }
}
