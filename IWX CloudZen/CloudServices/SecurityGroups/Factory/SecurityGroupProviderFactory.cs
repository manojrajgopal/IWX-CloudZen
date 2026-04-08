using IWX_CloudZen.CloudServices.SecurityGroups.Interfaces;
using IWX_CloudZen.CloudServices.SecurityGroups.Providers;

namespace IWX_CloudZen.CloudServices.SecurityGroups.Factory
{
    public static class SecurityGroupProviderFactory
    {
        public static ISecurityGroupProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsSecurityGroupProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported for Security Groups.")
            };
        }
    }
}
