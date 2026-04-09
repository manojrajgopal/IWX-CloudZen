using IWX_CloudZen.CloudServices.EC2InstanceConnect.Interfaces;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Providers;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Factory
{
    public class Ec2InstanceConnectProviderFactory
    {
        public static IEc2InstanceConnectProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsEc2InstanceConnectProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
