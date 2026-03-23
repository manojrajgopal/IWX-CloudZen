using Azure.Storage.Blobs;
using IWX_CloudZen.CloudStorage.Interfaces;
using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudStorage.Providers
{
    public class AzureStorageProvider : IFileStorageProvider
    {
        public const string container = "cloudzen";
        
        public async Task<string> UploadFile(CloudConnectionSecrets account, IFormFile file, string folder)
        {
            try
            {
                var connection = $"DefaultEndpointsProtocol=https;AccountName={account.AccountName};AccountKey={account.SecretKey};EndpointSuffix=core.windows.net";

                var client = new BlobContainerClient(connection, container);

                await client.CreateIfNotExistsAsync();

                var key = folder + "/" + Guid.NewGuid() + "_" + file.FileName;

                var blob = client.GetBlobClient(key);

                using var stream = file.OpenReadStream();

                await blob.UploadAsync(stream, true);

                return key;
            }
            catch (Exception ex)
            {
                throw new Exception("Upload failed: " + ex.Message);
            }
        }

        public async Task DeleteFile(CloudConnectionSecrets account, string fileUrl)
        {
            try
            {
                var connection = $"DefaultEndpointsProtocol=https;AccountName={account.AccountName};AccountKey={account.SecretKey};EndpointSuffix=core.windows.net";

                var client = new BlobContainerClient(connection, container);

                var blob = client.GetBlobClient(fileUrl);

                await blob.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Delete failed: " + ex.Message);
            }
        }

        public async Task<Stream> DownloadFile(CloudConnectionSecrets account, string fileUrl)
        {
            try
            {
                var connection = $"DefaultEndpointsProtocol=https;AccountName={account.AccountName};AccountKey={account.SecretKey};EndpointSuffix=core.windows.net";

                var client = new BlobContainerClient(connection, container);

                var blob = client.GetBlobClient(fileUrl);
                var memory = new MemoryStream();

                await blob.DownloadToAsync(memory);

                memory.Position = 0;

                return memory;
            }
            catch(Exception ex)
            {
                throw new Exception("Download failed: " + ex.Message);
            }
        }
    }
}
