using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudAccounts.Interfaces
{
    public interface ICloudProvider
    {
        Task<bool> ValidateConnectionAsync(ConnectCloudRequest request);
    }
}