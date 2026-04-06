using IWX_CloudZen.CloudServices.CloudStorage.Interfaces;

namespace IWX_CloudZen.CloudServices.CloudStorage.Providers
{
    public class StorageProviderFactory
    {
        public static IStorageProvider Get(string provider) => provider switch
        {
            "AWS" => new AwsS3Provider(),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
        };
    }
}
