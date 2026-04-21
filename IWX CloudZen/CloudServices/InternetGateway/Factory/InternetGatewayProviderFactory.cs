using IWX_CloudZen.CloudServices.InternetGateway.Interfaces;
using IWX_CloudZen.CloudServices.InternetGateway.Providers;

namespace IWX_CloudZen.CloudServices.InternetGateway.Factory
{
    public class InternetGatewayProviderFactory
    {
        public static IInternetGatewayProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsInternetGatewayProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
