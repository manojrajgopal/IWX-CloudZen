using IWX_CloudZen.CloudDeployments.Interfaces;
using IWX_CloudZen.CloudDeployments.Providers;

namespace IWX_CloudZen.CloudDeployments.Factory
{
    public class DeploymentProviderFactory
    {
        public static ICloudDeploymentProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsDeploymentProvider(),
                "AZURE" => new AzureDeploymentProvider(),
                _ => throw new Exception("Provider not supported")
            };
        }
    }
}
