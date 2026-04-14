using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.CloudStorage.DTOs;
using IWX_CloudZen.CloudServices.CloudStorage.Entities;
using IWX_CloudZen.CloudServices.CloudStorage.Providers;
using IWX_CloudZen.Data;
using IWX_CloudZen.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.CloudStorage.Services
{
    public class CloudStorageService
    {
        private readonly AppDbContext _db;
        private readonly CloudAccountService _accounts;

        public CloudStorageService(AppDbContext db, CloudAccountService accounts)
        {
            _db = db;
            _accounts = accounts;
        }

        // ---- Mappers ----

        private static BucketResponse MapBucket(BucketRecord r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Region = r.Region,
            Status = r.Status,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static FileResponse MapFile(CloudFile f) => new()
        {
            Id = f.Id,
            FileName = f.FileName,
            FileUrl = f.FileUrl,
            BucketName = f.BucketName,
            Folder = f.Folder,
            Size = f.Size,
            ContentType = f.ContentType,
            Provider = f.Provider,
            CloudAccountId = f.CloudAccountId,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        };

        // ---- Bucket CRUD ----

        public async Task<BucketListResponse> ListBuckets(string user, int accountId)
        {
            var records = await _db.BucketRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new BucketListResponse { Buckets = records.Select(MapBucket).ToList() };
        }

        // Normalizes a string to a valid S3 bucket name:
        // - Lowercase only
        // - Replace spaces/underscores/dots with hyphens
        // - Strip any character that is not a-z, 0-9, or hyphen
        // - Collapse consecutive hyphens into one
        // - Trim leading/trailing hyphens
        // - Enforce 3–63 character length
        // - Reject names that look like IP addresses
        public static string NormalizeBucketName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Bucket name cannot be empty.");

            var name = input.Trim().ToLowerInvariant();

            // Replace spaces, underscores, dots with hyphens
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[\s_.]", "-");

            // Remove any character that is not lowercase letter, digit, or hyphen
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-z0-9\-]", "");

            // Collapse consecutive hyphens
            name = System.Text.RegularExpressions.Regex.Replace(name, @"-{2,}", "-");

            // Trim leading/trailing hyphens
            name = name.Trim('-');

            if (name.Length < 3)
                throw new ArgumentException($"Bucket name '{input}' is too short after normalization (minimum 3 characters).");

            if (name.Length > 63)
                name = name[..63].TrimEnd('-');

            // Reject IP-address-like names (e.g. 192-168-1-1)
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d+\-\d+\-\d+\-\d+$"))
                throw new ArgumentException($"Bucket name '{name}' resembles an IP address, which is not allowed.");

            return name;
        }

        public async Task<BucketResponse> CreateBucket(string user, int accountId, string bucketName)
        {
            var normalizedName = CloudResourceNameNormalizer.NormalizeBucketName(bucketName);

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudResult = await provider.CreateBucket(account, normalizedName);

            var record = new BucketRecord
            {
                Name = normalizedName,
                Region = cloudResult.Region,
                Status = cloudResult.Status,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.BucketRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapBucket(record);
        }

        public async Task DeleteBucket(string user, int accountId, int bucketId)
        {
            var record = await _db.BucketRecords
                .FirstOrDefaultAsync(x => x.Id == bucketId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Bucket not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteBucket(account, record.Name);

            // Remove all DB file records for this bucket
            var files = await _db.CloudFiles
                .Where(f => f.BucketName == record.Name && f.CloudAccountId == accountId)
                .ToListAsync();

            _db.CloudFiles.RemoveRange(files);
            _db.BucketRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        public async Task<BucketSyncResult> SyncBuckets(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudBuckets = await provider.FetchAllBuckets(account);

            var dbRecords = await _db.BucketRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            // Added: in cloud but not in DB
            foreach (var cloud in cloudBuckets)
            {
                var existing = dbRecords.FirstOrDefault(r => r.Name == cloud.Name);

                if (existing is null)
                {
                    _db.BucketRecords.Add(new BucketRecord
                    {
                        Name = cloud.Name,
                        Region = cloud.Region,
                        Status = cloud.Status,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    // Updated: sync status/region
                    bool changed = existing.Status != cloud.Status || existing.Region != cloud.Region;
                    if (changed)
                    {
                        existing.Status = cloud.Status;
                        existing.Region = cloud.Region;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // Removed: in DB but no longer in cloud
            var cloudNames = cloudBuckets.Select(b => b.Name).ToHashSet();
            var toRemove = dbRecords.Where(r => !cloudNames.Contains(r.Name)).ToList();
            _db.BucketRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var final = await _db.BucketRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            return new BucketSyncResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Buckets = final.Select(MapBucket).ToList()
            };
        }

        // ---- File CRUD ----

        public async Task<FileListResponse> ListFiles(string user, int accountId, int? bucketId)
        {
            var query = _db.CloudFiles
                .Where(f => f.UploadedBy == user && f.CloudAccountId == accountId);

            if (bucketId.HasValue)
            {
                var bucket = await _db.BucketRecords.FindAsync(bucketId.Value);
                if (bucket != null)
                    query = query.Where(f => f.BucketName == bucket.Name);
            }

            var files = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
            return new FileListResponse { Files = files.Select(MapFile).ToList() };
        }

        public async Task<FileResponse> UploadFile(string user, int accountId, int bucketId, IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required.");

            var bucket = await _db.BucketRecords
                .FirstOrDefaultAsync(b => b.Id == bucketId && b.CloudAccountId == accountId)
                ?? throw new KeyNotFoundException("Bucket not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudResult = await provider.UploadFile(account, bucket.Name, file, folder);

            var entity = new CloudFile
            {
                FileName = file.FileName,
                FileUrl = cloudResult.Key,
                BucketName = bucket.Name,
                Provider = account.Provider!,
                Folder = folder,
                Size = file.Length,
                ContentType = file.ContentType,
                ETag = cloudResult.ETag,
                CloudAccountId = accountId,
                UploadedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.CloudFiles.Add(entity);
            await _db.SaveChangesAsync();

            return MapFile(entity);
        }

        public async Task<(Stream stream, string contentType, string fileName)> DownloadFile(string user, int fileId)
        {
            var file = await _db.CloudFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedBy == user)
                ?? throw new KeyNotFoundException("File not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, file.CloudAccountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var stream = await provider.DownloadFile(account, file.BucketName, file.FileUrl);
            return (stream, file.ContentType, file.FileName);
        }

        public async Task DeleteFile(string user, int fileId)
        {
            var file = await _db.CloudFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedBy == user)
                ?? throw new KeyNotFoundException("File not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, file.CloudAccountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteFile(account, file.BucketName, file.FileUrl);

            _db.CloudFiles.Remove(file);
            await _db.SaveChangesAsync();
        }

        public async Task<FileResponse> UpdateFile(string user, int fileId, IFormFile newFile)
        {
            if (newFile == null || newFile.Length == 0)
                throw new ArgumentException("File is required.");

            var file = await _db.CloudFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UploadedBy == user)
                ?? throw new KeyNotFoundException("File not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, file.CloudAccountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudResult = await provider.ReplaceFile(account, file.BucketName, file.FileUrl, newFile);

            file.FileName = newFile.FileName;
            file.Size = newFile.Length;
            file.ContentType = newFile.ContentType;
            file.ETag = cloudResult.ETag;
            file.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapFile(file);
        }

        public async Task<FileSyncResult> SyncFiles(string user, int accountId, int bucketId)
        {
            var bucket = await _db.BucketRecords
                .FirstOrDefaultAsync(b => b.Id == bucketId && b.CloudAccountId == accountId)
                ?? throw new KeyNotFoundException("Bucket not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudFiles = await provider.FetchAllFiles(account, bucket.Name);

            var dbFiles = await _db.CloudFiles
                .Where(f => f.BucketName == bucket.Name && f.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            // Added / Updated
            foreach (var cloud in cloudFiles)
            {
                var existing = dbFiles.FirstOrDefault(f => f.FileUrl == cloud.Key);

                if (existing is null)
                {
                    _db.CloudFiles.Add(new CloudFile
                    {
                        FileName = cloud.FileName,
                        FileUrl = cloud.Key,
                        BucketName = bucket.Name,
                        Provider = account.Provider!,
                        Folder = Path.GetDirectoryName(cloud.Key)?.Replace('\\', '/') ?? string.Empty,
                        Size = cloud.Size,
                        ContentType = cloud.ContentType,
                        ETag = cloud.ETag,
                        CloudAccountId = accountId,
                        UploadedBy = user,
                        CreatedAt = cloud.LastModified
                    });
                    added++;
                }
                else
                {
                    bool changed = existing.ETag != cloud.ETag || existing.Size != cloud.Size;
                    if (changed)
                    {
                        existing.Size = cloud.Size;
                        existing.ETag = cloud.ETag;
                        existing.UpdatedAt = cloud.LastModified;
                        updated++;
                    }
                }
            }

            // Removed: in DB but no longer in S3
            var cloudKeys = cloudFiles.Select(f => f.Key).ToHashSet();
            var toRemove = dbFiles.Where(f => !cloudKeys.Contains(f.FileUrl)).ToList();
            _db.CloudFiles.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var final = await _db.CloudFiles
                .Where(f => f.BucketName == bucket.Name && f.CloudAccountId == accountId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return new FileSyncResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Files = final.Select(MapFile).ToList()
            };
        }

        public async Task<FullSyncResult> SyncAll(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = StorageProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            // ---- Step 1: sync buckets ----
            var cloudBuckets = await provider.FetchAllBuckets(account);

            var dbBuckets = await _db.BucketRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int bucketsAdded = 0, bucketsUpdated = 0, bucketsRemoved = 0;

            foreach (var cloud in cloudBuckets)
            {
                var existing = dbBuckets.FirstOrDefault(r => r.Name == cloud.Name);
                if (existing is null)
                {
                    _db.BucketRecords.Add(new BucketRecord
                    {
                        Name = cloud.Name,
                        Region = cloud.Region,
                        Status = cloud.Status,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    });
                    bucketsAdded++;
                }
                else
                {
                    bool changed = existing.Status != cloud.Status || existing.Region != cloud.Region;
                    if (changed)
                    {
                        existing.Status = cloud.Status;
                        existing.Region = cloud.Region;
                        existing.UpdatedAt = DateTime.UtcNow;
                        bucketsUpdated++;
                    }
                }
            }

            var cloudBucketNames = cloudBuckets.Select(b => b.Name).ToHashSet();
            var bucketsToRemove = dbBuckets.Where(r => !cloudBucketNames.Contains(r.Name)).ToList();
            _db.BucketRecords.RemoveRange(bucketsToRemove);
            bucketsRemoved = bucketsToRemove.Count;

            await _db.SaveChangesAsync();

            // ---- Step 2: fetch all files from AWS in parallel (no DB access) ----
            var allBucketRecords = await _db.BucketRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            var fetchTasks = allBucketRecords.Select(async bucket =>
            {
                var cloudFiles = await provider.FetchAllFiles(account, bucket.Name);
                return (bucket, cloudFiles);
            }).ToList();

            var fetchResults = await Task.WhenAll(fetchTasks);

            // ---- Step 3: sync files with DB sequentially (DbContext is not thread-safe) ----
            var fileSyncResults = new List<(BucketRecord bucket, int filesAdded, int filesUpdated, int filesRemoved)>();

            foreach (var (bucket, cloudFiles) in fetchResults)
            {
                var dbFiles = await _db.CloudFiles
                    .Where(f => f.BucketName == bucket.Name && f.CloudAccountId == accountId)
                    .ToListAsync();

                int filesAdded = 0, filesUpdated = 0, filesRemoved = 0;

                foreach (var cloud in cloudFiles)
                {
                    var existing = dbFiles.FirstOrDefault(f => f.FileUrl == cloud.Key);
                    if (existing is null)
                    {
                        _db.CloudFiles.Add(new CloudFile
                        {
                            FileName = cloud.FileName,
                            FileUrl = cloud.Key,
                            BucketName = bucket.Name,
                            Provider = account.Provider!,
                            Folder = Path.GetDirectoryName(cloud.Key)?.Replace('\\', '/') ?? string.Empty,
                            Size = cloud.Size,
                            ContentType = cloud.ContentType,
                            ETag = cloud.ETag,
                            CloudAccountId = accountId,
                            UploadedBy = user,
                            CreatedAt = cloud.LastModified
                        });
                        filesAdded++;
                    }
                    else
                    {
                        bool changed = existing.ETag != cloud.ETag || existing.Size != cloud.Size;
                        if (changed)
                        {
                            existing.Size = cloud.Size;
                            existing.ETag = cloud.ETag;
                            existing.UpdatedAt = cloud.LastModified;
                            filesUpdated++;
                        }
                    }
                }

                var cloudKeys = cloudFiles.Select(f => f.Key).ToHashSet();
                var filesToRemove = dbFiles.Where(f => !cloudKeys.Contains(f.FileUrl)).ToList();
                _db.CloudFiles.RemoveRange(filesToRemove);
                filesRemoved = filesToRemove.Count;

                fileSyncResults.Add((bucket, filesAdded, filesUpdated, filesRemoved));
            }

            await _db.SaveChangesAsync();

            // ---- Build response ----
            var finalBucketRecords = await _db.BucketRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            var bucketResults = new List<BucketFileSyncResult>();

            foreach (var (bucket, filesAdded, filesUpdated, filesRemoved) in fileSyncResults)
            {
                var finalFiles = await _db.CloudFiles
                    .Where(f => f.BucketName == bucket.Name && f.CloudAccountId == accountId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                bucketResults.Add(new BucketFileSyncResult
                {
                    Bucket = MapBucket(bucket),
                    FilesAdded = filesAdded,
                    FilesUpdated = filesUpdated,
                    FilesRemoved = filesRemoved,
                    Files = finalFiles.Select(MapFile).ToList()
                });
            }

            return new FullSyncResult
            {
                BucketsAdded = bucketsAdded,
                BucketsUpdated = bucketsUpdated,
                BucketsRemoved = bucketsRemoved,
                Buckets = bucketResults
            };
        }
    }
}
