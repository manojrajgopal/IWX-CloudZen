using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudStorage.Interfaces
{
    public interface IFileStorageProvider
    {
        Task<string> UploadFile(CloudConnectionSecrets account, IFormFile file, string folder);

        Task DeleteFile(CloudConnectionSecrets account, string fileUrl);
    }
}
