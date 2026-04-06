using IWX_CloudZen.CloudServiceCreation.Interfaces;
using IWX_CloudZen.CloudServiceCreation.Providers;

namespace IWX_CloudZen.CloudServiceCreation.Factory
{
    public class StorageServiceFactory
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