namespace IWX_CloudZen.CloudAccounts.Interfaces
{
    public interface ICloudProvider
    {
        Task<bool> ValidateConnection(
            CloudAccounts.Entities.CloudAccount account
        );

        Task<List<string>> GetStorageList(
            CloudAccounts.Entities.CloudAccount account
        );
    }
}
