using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.CloudAccounts.Providers;

namespace IWX_CloudZen.CloudAccounts.Factory
{
    public static class CloudProviderFactory
    {
        public static ICloudProvider GetProvider(string provider)
        {
            return provider.Trim().ToUpperInvariant() switch
            {
                "AWS" => new AwsProvider(),
                "AZURE" => new AzureProvider(),
                _ => throw new NotSupportedException("Provider not supported")
            };
        }

        public static List<CloudProviderOption> GetSupportedProviders()
        {
            return new List<CloudProviderOption>
            {
                new CloudProviderOption
                {
                    Value = "AWS",
                    Label = "Amazon Web Services",
                    RequiredFields = ["AccessKey", "SecretKey", "Region"]
                },
                new CloudProviderOption
                {
                    Value = "Azure",
                    Label = "Microsoft Azure",
                    RequiredFields = ["TenantId", "ClientId", "ClientSecret"]
                }
            };
        }
    }
}