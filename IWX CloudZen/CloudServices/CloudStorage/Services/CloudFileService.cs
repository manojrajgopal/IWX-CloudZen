using IWX_CloudZen.Data;
using IWX_CloudZen.CloudServices.CloudStorage.Entities;
using IWX_CloudZen.CloudServices.CloudStorage.Providers;
using IWX_CloudZen.CloudAccounts.Services;

namespace IWX_CloudZen.CloudServices.CloudStorage.Services
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

        public List<CloudFile> GetFiles(string user)
        {
            return _context.CloudFiles.Where(x => x.UploadedBy == user).OrderByDescending(x => x.CreatedAt).ToList();
        }

        public List<string> GetFolders(string user)
        {
            return _context.CloudFiles.Where(x => x.UploadedBy == user).Select(x => x.Folder).Distinct().ToList();
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

        public async Task<(Stream, string, string)> Download(string user, int fileId)
        {
            try
            {
                var file = _context.CloudFiles.FirstOrDefault(x => x.Id == fileId && x.UploadedBy == user);

                if (file == null)
                    throw new Exception("File not found");

                var account = await _accounts.ResolveCredentialsAsync(user, file.CloudAccountId);

                var provider = StorageProviderFactory.GetProvider(file.Provider);

                var stream = await provider.DownloadFile(account, file.FileUrl);

                return (stream, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                throw new Exception("Download failed: " + ex.Message);
            }
        }

        public async Task Delete(string user, int fileId)
        {
            try
            {
                var file = _context.CloudFiles.FirstOrDefault(x => x.Id == fileId && x.UploadedBy == user);

                if (file == null)
                    throw new Exception("File not found");

                var account = await _accounts.ResolveCredentialsAsync(user, file.CloudAccountId);

                var provider = StorageProviderFactory.GetProvider(file.Provider);

                await provider.DeleteFile(account, file.FileUrl);

                _context.CloudFiles.Remove(file);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Delete failed: " + ex.Message);
            }
        }

        public async Task<CloudFile> UpdateFile(string user, int fileId, IFormFile newFile)
        {
            try
            {
                var file = _context.CloudFiles.FirstOrDefault(x => x.Id == fileId && x.UploadedBy == user);

                if (file == null)
                    throw new Exception("File not found");

                var account = await _accounts.ResolveCredentialsAsync(user, file.CloudAccountId);

                var provider = StorageProviderFactory.GetProvider(file.Provider);

                await provider.DeleteFile(account, file.FileUrl);

                var newUrl = await provider.UploadFile(account, newFile, file.Folder);

                file.FileName = newFile.FileName;

                file.FileUrl = newUrl;

                file.Size = newFile.Length;

                await _context.SaveChangesAsync();

                return file;
            }
            catch (Exception ex)
            {
                throw new Exception("Update file failed: " + ex.Message);
            }
        }
    }
}
