using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.CloudStorage.DTOs;

namespace IWX_CloudZen.CloudServices.CloudStorage.Interfaces
{
    public interface IStorageProvider
    {
        // Buckets
        Task<CloudBucketInfo> CreateBucket(CloudConnectionSecrets account, string bucketName);
        Task DeleteBucket(CloudConnectionSecrets account, string bucketName);
        Task<List<CloudBucketInfo>> FetchAllBuckets(CloudConnectionSecrets account);

        // Files
        Task<CloudFileInfo> UploadFile(CloudConnectionSecrets account, string bucketName, IFormFile file, string folder);
        Task<Stream> DownloadFile(CloudConnectionSecrets account, string bucketName, string fileKey);
        Task DeleteFile(CloudConnectionSecrets account, string bucketName, string fileKey);
        Task<CloudFileInfo> ReplaceFile(CloudConnectionSecrets account, string bucketName, string fileKey, IFormFile newFile);
        Task<List<CloudFileInfo>> FetchAllFiles(CloudConnectionSecrets account, string bucketName);
    }
}
