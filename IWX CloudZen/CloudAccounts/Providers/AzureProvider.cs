using Azure.Identity;
using Azure.ResourceManager;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Interfaces;

namespace IWX_CloudZen.CloudAccounts.Providers
{
    public class AzureProvider : ICloudProvider
    {
        public async Task<bool> ValidateConnectionAsync(ConnectCloudRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenantId) ||
                string.IsNullOrWhiteSpace(request.ClientId) ||
                string.IsNullOrWhiteSpace(request.ClientSecret))
            {
                return false;
            }

            try
            {
                var credential = new ClientSecretCredential(
                    request.TenantId,
                    request.ClientId,
                    request.ClientSecret);

                var arm = new ArmClient(credential);

                await Task.CompletedTask;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}