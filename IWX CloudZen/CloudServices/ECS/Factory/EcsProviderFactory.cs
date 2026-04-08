using IWX_CloudZen.CloudServices.ECS.Interfaces;
using IWX_CloudZen.CloudServices.ECS.Providers;

namespace IWX_CloudZen.CloudServices.ECS.Factory
{
    public static class EcsProviderFactory
    {
        public static IEcsProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsEcsProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported for ECS.")
            };
        }
    }
}
