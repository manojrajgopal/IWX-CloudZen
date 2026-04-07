using IWX_CloudZen.Permissions.Interfaces;
using IWX_CloudZen.Permissions.Providers;

namespace IWX_CloudZen.Permissions.Factory
{
    public class PermissionsProviderFactory
    {
        public static IPermissionsProvider Get(string provider)
        {
            return provider switch
            {
                "AWS" => new AwsPermissionsProvider(),
                _ => throw new NotSupportedException($"Provider '{provider}' is not supported.")
            };
        }
    }
}
