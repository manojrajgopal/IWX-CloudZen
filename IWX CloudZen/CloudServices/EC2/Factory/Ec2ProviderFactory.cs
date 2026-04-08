using IWX_CloudZen.CloudServices.EC2.Interfaces;
using IWX_CloudZen.CloudServices.EC2.Providers;

namespace IWX_CloudZen.CloudServices.EC2.Factory
{
    public class Ec2ProviderFactory
    {
        public static IEc2Provider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsEc2Provider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
