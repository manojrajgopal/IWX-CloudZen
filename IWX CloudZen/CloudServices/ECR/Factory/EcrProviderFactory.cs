using IWX_CloudZen.CloudServices.ECR.Interfaces;
using IWX_CloudZen.CloudServices.ECR.Providers;

namespace IWX_CloudZen.CloudServices.ECR.Factory
{
    public class EcrProviderFactory
    {
        public static IEcrProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsEcrProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
