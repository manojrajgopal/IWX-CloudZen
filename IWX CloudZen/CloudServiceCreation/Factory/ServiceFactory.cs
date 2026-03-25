using IWX_CloudZen.CloudServiceCreation.Interfaces;
using IWX_CloudZen.CloudServiceCreation.Providers;

namespace IWX_CloudZen.CloudServiceCreation.Factory
{
    public class ServiceFactory
    {
        public static ICloudServiceCreator Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsServiceCreator(),
                _ => throw new Exception("Provider not supported")
            };
        }
    }
}
