using IWX_CloudZen.CloudServices.VPC.Interfaces;
using IWX_CloudZen.CloudServices.VPC.Providers;

namespace IWX_CloudZen.CloudServices.VPC.Factory
{
    public class VpcProviderFactory
    {
        public static IVpcProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsVpcProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
