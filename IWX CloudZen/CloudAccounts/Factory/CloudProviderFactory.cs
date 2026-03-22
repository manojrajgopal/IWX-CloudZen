using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.CloudAccounts.Providers;

namespace IWX_CloudZen.CloudAccounts.Factory
{
    public class CloudProviderFactory
    {
        public static ICloudProvider
        GetProvider(string provider)
        {

            return provider switch
            {

                "AWS" => new AwsProvider(),

                "Azure" => new AzureProvider(),

                _ => throw new Exception(
                    "Provider not supported")

            };

        }
    }
}
