using IWX_CloudZen.Data;
using IWX_CloudZen.CloudStorage.Entities;
using IWX_CloudZen.CloudStorage.Providers;
using IWX_CloudZen.CloudAccounts.Services;

namespace IWX_CloudZen.CloudStorage.Services
{
    public class CloudFileService
    {
        private readonly AppDbContext _context;
        private readonly CloudAccountService _accounts;

        public CloudFileService(AppDbContext context, CloudAccountService accounts)
        {
            _context = context;
            _accounts = accounts;
        }

        public async Task<CloudFile> Upload(string user, IFormFile file, string folder, int accountId)
        {
            if (file == null)
                throw new Exception("File Missing");

            if (string.IsNullOrEmpty(folder))
                throw new Exception("Folder required");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId);
            
            if (account == null)
                throw new Exception("Cloud account not found");

            var provider = StorageProviderFactory.GetProvider(account.Provider);

            var url = await provider.UploadFile(account, file, folder);

            var entity = new CloudFile
            {
                FileName = file.FileName,
                FileUrl = url,
                Provider = account.Provider,
                Folder = folder,
                Size = file.Length,
                ContentType = file.ContentType,
                CloudAccountId = accountId,
                UploadedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _context.CloudFiles.Add(entity);

            await _context.SaveChangesAsync();

            return entity;
        }
    }
}
