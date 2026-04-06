using IWX_CloudZen.CloudServices.Cluster.Interfaces;
using IWX_CloudZen.CloudServices.Cluster.Providers;

namespace IWX_CloudZen.CloudServices.Cluster.Factory
{
    public class ClusterProviderFactory
    {
        public static IClusterProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsClusterProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
