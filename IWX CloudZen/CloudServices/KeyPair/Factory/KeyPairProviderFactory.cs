using IWX_CloudZen.CloudServices.KeyPair.Interfaces;
using IWX_CloudZen.CloudServices.KeyPair.Providers;

namespace IWX_CloudZen.CloudServices.KeyPair.Factory
{
    public class KeyPairProviderFactory
    {
        public static IKeyPairProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsKeyPairProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
