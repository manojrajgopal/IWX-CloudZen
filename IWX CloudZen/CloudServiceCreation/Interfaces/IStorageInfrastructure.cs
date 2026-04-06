using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudServiceCreation.Interfaces
{
    public interface IStorageInfrastructure
    {
        Task<string> CreateBucket(CloudConnectionSecrets account, string bucketName);
    }
}
