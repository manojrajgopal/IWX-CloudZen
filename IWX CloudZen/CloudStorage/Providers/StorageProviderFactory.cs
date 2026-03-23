using IWX_CloudZen.CloudStorage.Interfaces;

namespace IWX_CloudZen.CloudStorage.Providers
{
    public class StorageProviderFactory
    {
        public static IFileStorageProvider GetProvider(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsStorageProvider(),

                "AZURE" => new AzureStorageProvider(),

                _ => throw new Exception("Provider not supported")
            };
        }
    }
}
