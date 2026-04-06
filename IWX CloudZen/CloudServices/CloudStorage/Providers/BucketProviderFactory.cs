using IWX_CloudZen.CloudServices.CloudStorage.Interfaces;

namespace IWX_CloudZen.CloudServices.CloudStorage.Providers
{
    public class BucketProviderFactory
    {
        public static IStorageInfrastructure Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsS3ServiceCreator(),
                _ => throw new Exception("Provider not supported")
            };
        }
    }
}
