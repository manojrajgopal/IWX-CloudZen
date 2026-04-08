using IWX_CloudZen.CloudServices.CloudWatchLogs.Interfaces;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Providers;

namespace IWX_CloudZen.CloudServices.CloudWatchLogs.Factory
{
    public class CloudWatchLogsProviderFactory
    {
        public static ICloudWatchLogsProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsCloudWatchLogsProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported for CloudWatch Logs.")
            };
        }
    }
}
