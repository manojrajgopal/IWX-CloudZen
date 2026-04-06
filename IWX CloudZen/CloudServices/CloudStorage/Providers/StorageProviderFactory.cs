using IWX_CloudZen.CloudServices.CloudStorage.Interfaces;

namespace IWX_CloudZen.CloudServices.CloudStorage.Providers
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
