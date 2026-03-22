using Azure.Identity;
using Azure.ResourceManager;
using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.CloudAccounts.Entities;

namespace IWX_CloudZen.CloudAccounts.Providers
{
    public class AzureProvider : ICloudProvider
    {

        public async Task<bool>
        ValidateConnection(CloudAccount account)
        {

            try
            {

                var credential =
                new ClientSecretCredential(

                    account.TenantId,

                    account.ClientId,

                    account.ClientSecret
                );

                var arm =
                new ArmClient(credential);

                return true;

            }
            catch
            {

                return false;
            }

        }

        public async Task<List<string>>
        GetStorageList(CloudAccount account)
        {
            return new List<string>();
        }

    }
}
