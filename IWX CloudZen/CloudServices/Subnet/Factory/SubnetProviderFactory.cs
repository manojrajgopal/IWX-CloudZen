using IWX_CloudZen.CloudServices.Subnet.Interfaces;
using IWX_CloudZen.CloudServices.Subnet.Providers;

namespace IWX_CloudZen.CloudServices.Subnet.Factory
{
    public static class SubnetProviderFactory
    {
        public static ISubnetProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsSubnetProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported for Subnets.")
            };
        }
    }
}
