using IWX_CloudZen.CloudServices.Mapped.Interfaces;
using IWX_CloudZen.CloudServices.Mapped.Providers;

namespace IWX_CloudZen.CloudServices.Mapped.Factory
{
    public class MappedProviderFactory
    {
        public static IMappedProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsMappedProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
