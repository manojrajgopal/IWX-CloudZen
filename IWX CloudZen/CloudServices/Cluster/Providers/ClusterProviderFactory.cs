using IWX_CloudZen.CloudServices.Cluster.Interfaces;

namespace IWX_CloudZen.CloudServices.Cluster.Providers
{
    public class ClusterProviderFactory
    {
        public static IClusterProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsClusterProvider(),
                _ => throw new Exception("Provider not supported")
            };
        }
    }
}
