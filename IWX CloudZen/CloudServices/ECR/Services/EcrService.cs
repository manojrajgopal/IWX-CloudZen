using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.ECR.DTOs;
using IWX_CloudZen.CloudServices.ECR.Entities;
using IWX_CloudZen.CloudServices.ECR.Factory;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace IWX_CloudZen.CloudServices.ECR.Services
{
    public class EcrService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public EcrService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ---- Normalizer ----

        /// <summary>
        /// Normalizes a repository name to satisfy the AWS ECR constraint:
        /// [a-z0-9]+((\.  |_|__|-+)[a-z0-9]+)*(/[a-z0-9]+((\.  |_|__|-+)[a-z0-9]+)*)*
        /// </summary>
        public static string NormalizeRepositoryName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Repository name cannot be empty.");

            // Lowercase
            var normalized = name.ToLowerInvariant();

            // Replace whitespace with dash
            normalized = Regex.Replace(normalized, @"\s+", "-");

            // Replace any character not valid in ECR names with dash
            normalized = Regex.Replace(normalized, @"[^a-z0-9._/\-]", "-");

            // Collapse multiple consecutive dashes into one
            normalized = Regex.Replace(normalized, @"-{2,}", "-");

            // Clean up each path segment (split on /)
            var segments = normalized
                .Split('/')
                .Select(seg => seg.Trim('-', '.', '_'))
                .Where(seg => seg.Length > 0)
                .ToArray();

            if (segments.Length == 0)
                throw new ArgumentException($"'{name}' could not be normalized to a valid ECR repository name.");

            normalized = string.Join("/", segments);

            // Ensure starts and ends with alphanumeric
            normalized = Regex.Replace(normalized, @"^[^a-z0-9]+", "");
            normalized = Regex.Replace(normalized, @"[^a-z0-9]+$", "");

            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException($"'{name}' could not be normalized to a valid ECR repository name.");

            return normalized;
        }

        // ---- Mappers ----

        private static RepositoryResponse MapRepo(EcrRepositoryRecord r) => new()
        {
            Id = r.Id,
            RepositoryName = r.RepositoryName,
            RepositoryArn = r.RepositoryArn,
            RepositoryUri = r.RepositoryUri,
            ImageTagMutability = r.ImageTagMutability,
            ScanOnPush = r.ScanOnPush,
            EncryptionType = r.EncryptionType,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static ImageResponse MapImage(EcrImageRecord r) => new()
        {
            Id = r.Id,
            RepositoryRecordId = r.RepositoryRecordId,
            RepositoryName = r.RepositoryName,
            ImageTag = r.ImageTag,
            ImageDigest = r.ImageDigest,
            SizeInBytes = r.SizeInBytes,
            ScanStatus = r.ScanStatus,
            Findings = (r.FindingsCritical.HasValue || r.FindingsHigh.HasValue)
                ? new ImageFindingSummary
                {
                    Critical = r.FindingsCritical ?? 0,
                    High = r.FindingsHigh ?? 0,
                    Medium = r.FindingsMedium ?? 0,
                    Low = r.FindingsLow ?? 0
                }
                : null,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            PushedAt = r.PushedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private async Task<(CloudAccounts.DTOs.CloudConnectionSecrets account,
            Interfaces.IEcrProvider provider)> Resolve(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = EcrProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return (account, provider);
        }

        // ================================================================
        // REPOSITORY OPERATIONS
        // ================================================================

        public async Task<RepositoryListResponse> ListRepositories(string user, int accountId)
        {
            var records = await _db.EcrRepositoryRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderBy(x => x.RepositoryName)
                .ToListAsync();

            return new RepositoryListResponse
            {
                TotalRepositories = records.Count,
                Repositories = records.Select(MapRepo).ToList()
            };
        }

        public async Task<RepositoryResponse> GetRepository(string user, int accountId, int repoId)
        {
            var record = await _db.EcrRepositoryRecords
                .FirstOrDefaultAsync(x => x.Id == repoId &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Repository not found.");

            return MapRepo(record);
        }

        public async Task<RepositoryResponse> CreateRepository(
            string user, int accountId, CreateRepositoryRequest request)
        {
            var (account, provider) = await Resolve(user, accountId);

            var repositoryName = NormalizeRepositoryName(request.RepositoryName);

            var info = await provider.CreateRepository(
                account,
                repositoryName,
                request.ImageTagMutability,
                request.ScanOnPush,
                request.EncryptionType);

            var record = new EcrRepositoryRecord
            {
                RepositoryName = info.RepositoryName,
                RepositoryArn = info.RepositoryArn,
                RepositoryUri = info.RepositoryUri,
                ImageTagMutability = info.ImageTagMutability,
                ScanOnPush = info.ScanOnPush,
                EncryptionType = info.EncryptionType,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.EcrRepositoryRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapRepo(record);
        }

        public async Task<RepositoryResponse> UpdateRepository(
            string user, int accountId, int repoId, UpdateRepositoryRequest request)
        {
            var record = await _db.EcrRepositoryRecords
                .FirstOrDefaultAsync(x => x.Id == repoId &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Repository not found.");

            var (account, provider) = await Resolve(user, accountId);

            var info = await provider.UpdateRepository(
                account,
                record.RepositoryName,
                request.ImageTagMutability,
                request.ScanOnPush);

            record.ImageTagMutability = info.ImageTagMutability;
            record.ScanOnPush = info.ScanOnPush;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return MapRepo(record);
        }

        public async Task DeleteRepository(string user, int accountId, int repoId, bool force = true)
        {
            var record = await _db.EcrRepositoryRecords
                .FirstOrDefaultAsync(x => x.Id == repoId &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Repository not found.");

            var (account, provider) = await Resolve(user, accountId);

            await provider.DeleteRepository(account, record.RepositoryName, force);

            // Remove all images for this repository from DB
            var images = await _db.EcrImageRecords
                .Where(x => x.RepositoryRecordId == repoId)
                .ToListAsync();

            _db.EcrImageRecords.RemoveRange(images);
            _db.EcrRepositoryRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // IMAGE OPERATIONS
        // ================================================================

        public async Task<ImageListResponse> ListImages(string user, int accountId, int repoId)
        {
            var repo = await _db.EcrRepositoryRecords
                .FirstOrDefaultAsync(x => x.Id == repoId &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Repository not found.");

            var images = await _db.EcrImageRecords
                .Where(x => x.RepositoryRecordId == repoId && x.CloudAccountId == accountId)
                .OrderByDescending(x => x.PushedAt)
                .ToListAsync();

            return new ImageListResponse
            {
                RepositoryName = repo.RepositoryName,
                TotalImages = images.Count,
                Images = images.Select(MapImage).ToList()
            };
        }

        public async Task<ImageResponse> GetImage(string user, int accountId, int imageId)
        {
            var record = await _db.EcrImageRecords
                .FirstOrDefaultAsync(x => x.Id == imageId && x.CloudAccountId == accountId)
                ?? throw new KeyNotFoundException("Image not found.");

            return MapImage(record);
        }

        public async Task DeleteImage(string user, int accountId, int imageId)
        {
            var record = await _db.EcrImageRecords
                .FirstOrDefaultAsync(x => x.Id == imageId && x.CloudAccountId == accountId)
                ?? throw new KeyNotFoundException("Image not found.");

            var (account, provider) = await Resolve(user, accountId);

            if (string.IsNullOrEmpty(record.ImageDigest))
                throw new InvalidOperationException("Image digest is missing; cannot delete.");

            await provider.DeleteImage(account, record.RepositoryName, record.ImageDigest);

            _db.EcrImageRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // SYNC OPERATIONS
        // ================================================================

        public async Task<SyncRepositoriesResult> SyncRepositories(string user, int accountId)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudRepos = await provider.FetchAllRepositories(account);

            var dbRepos = await _db.EcrRepositoryRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudRepos)
            {
                var existing = dbRepos.FirstOrDefault(r => r.RepositoryName == cloud.RepositoryName);

                if (existing is null)
                {
                    _db.EcrRepositoryRecords.Add(new EcrRepositoryRecord
                    {
                        RepositoryName = cloud.RepositoryName,
                        RepositoryArn = cloud.RepositoryArn,
                        RepositoryUri = cloud.RepositoryUri,
                        ImageTagMutability = cloud.ImageTagMutability,
                        ScanOnPush = cloud.ScanOnPush,
                        EncryptionType = cloud.EncryptionType,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = cloud.CreatedAt ?? DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    bool changed =
                        existing.ImageTagMutability != cloud.ImageTagMutability ||
                        existing.ScanOnPush != cloud.ScanOnPush ||
                        existing.RepositoryUri != cloud.RepositoryUri;

                    if (changed)
                    {
                        existing.ImageTagMutability = cloud.ImageTagMutability;
                        existing.ScanOnPush = cloud.ScanOnPush;
                        existing.RepositoryUri = cloud.RepositoryUri;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudNames = cloudRepos.Select(r => r.RepositoryName).ToHashSet();
            var toRemove = dbRepos.Where(r => !cloudNames.Contains(r.RepositoryName)).ToList();

            // Remove images for deleted repositories
            foreach (var repo in toRemove)
            {
                var repoImages = await _db.EcrImageRecords
                    .Where(x => x.RepositoryRecordId == repo.Id)
                    .ToListAsync();
                _db.EcrImageRecords.RemoveRange(repoImages);
            }

            _db.EcrRepositoryRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.EcrRepositoryRecords
                .Where(x => x.CloudAccountId == accountId)
                .OrderBy(x => x.RepositoryName)
                .ToListAsync();

            return new SyncRepositoriesResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Repositories = finalRecords.Select(MapRepo).ToList()
            };
        }

        public async Task<SyncImagesResult> SyncImages(string user, int accountId, int repoId)
        {
            var repo = await _db.EcrRepositoryRecords
                .FirstOrDefaultAsync(x => x.Id == repoId &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Repository not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudImages = await provider.FetchAllImages(account, repo.RepositoryName);

            var dbImages = await _db.EcrImageRecords
                .Where(x => x.RepositoryRecordId == repoId && x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudImages)
            {
                var existing = dbImages.FirstOrDefault(i => i.ImageDigest == cloud.ImageDigest);

                if (existing is null)
                {
                    _db.EcrImageRecords.Add(new EcrImageRecord
                    {
                        RepositoryRecordId = repoId,
                        RepositoryName = repo.RepositoryName,
                        ImageTag = cloud.ImageTag,
                        ImageDigest = cloud.ImageDigest,
                        SizeInBytes = cloud.SizeInBytes,
                        ScanStatus = cloud.ScanStatus,
                        FindingsCritical = cloud.FindingsCritical,
                        FindingsHigh = cloud.FindingsHigh,
                        FindingsMedium = cloud.FindingsMedium,
                        FindingsLow = cloud.FindingsLow,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        PushedAt = cloud.PushedAt,
                        CreatedAt = DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    bool changed =
                        existing.ScanStatus != cloud.ScanStatus ||
                        existing.FindingsCritical != cloud.FindingsCritical ||
                        existing.FindingsHigh != cloud.FindingsHigh ||
                        existing.ImageTag != cloud.ImageTag;

                    if (changed)
                    {
                        existing.ScanStatus = cloud.ScanStatus;
                        existing.FindingsCritical = cloud.FindingsCritical;
                        existing.FindingsHigh = cloud.FindingsHigh;
                        existing.FindingsMedium = cloud.FindingsMedium;
                        existing.FindingsLow = cloud.FindingsLow;
                        existing.ImageTag = cloud.ImageTag;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudDigests = cloudImages.Select(i => i.ImageDigest).ToHashSet();
            var toRemove = dbImages.Where(i => string.IsNullOrEmpty(i.ImageDigest) || !cloudDigests.Contains(i.ImageDigest)).ToList();
            _db.EcrImageRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalImages = await _db.EcrImageRecords
                .Where(x => x.RepositoryRecordId == repoId && x.CloudAccountId == accountId)
                .OrderByDescending(x => x.PushedAt)
                .ToListAsync();

            return new SyncImagesResult
            {
                RepositoryName = repo.RepositoryName,
                Added = added,
                Updated = updated,
                Removed = removed,
                Images = finalImages.Select(MapImage).ToList()
            };
        }

        public async Task<FullEcrSyncResult> SyncAll(string user, int accountId)
        {
            var (account, provider) = await Resolve(user, accountId);

            // ---- Step 1: Sync repositories ----
            var cloudRepos = await provider.FetchAllRepositories(account);

            var dbRepos = await _db.EcrRepositoryRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int reposAdded = 0, reposUpdated = 0, reposRemoved = 0;

            foreach (var cloud in cloudRepos)
            {
                var existing = dbRepos.FirstOrDefault(r => r.RepositoryName == cloud.RepositoryName);

                if (existing is null)
                {
                    _db.EcrRepositoryRecords.Add(new EcrRepositoryRecord
                    {
                        RepositoryName = cloud.RepositoryName,
                        RepositoryArn = cloud.RepositoryArn,
                        RepositoryUri = cloud.RepositoryUri,
                        ImageTagMutability = cloud.ImageTagMutability,
                        ScanOnPush = cloud.ScanOnPush,
                        EncryptionType = cloud.EncryptionType,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = cloud.CreatedAt ?? DateTime.UtcNow
                    });
                    reposAdded++;
                }
                else
                {
                    bool changed =
                        existing.ImageTagMutability != cloud.ImageTagMutability ||
                        existing.ScanOnPush != cloud.ScanOnPush ||
                        existing.RepositoryUri != cloud.RepositoryUri;

                    if (changed)
                    {
                        existing.ImageTagMutability = cloud.ImageTagMutability;
                        existing.ScanOnPush = cloud.ScanOnPush;
                        existing.RepositoryUri = cloud.RepositoryUri;
                        existing.UpdatedAt = DateTime.UtcNow;
                        reposUpdated++;
                    }
                }
            }

            var cloudNames = cloudRepos.Select(r => r.RepositoryName).ToHashSet();
            var reposToRemove = dbRepos.Where(r => !cloudNames.Contains(r.RepositoryName)).ToList();

            foreach (var repo in reposToRemove)
            {
                var repoImages = await _db.EcrImageRecords
                    .Where(x => x.RepositoryRecordId == repo.Id)
                    .ToListAsync();
                _db.EcrImageRecords.RemoveRange(repoImages);
            }

            _db.EcrRepositoryRecords.RemoveRange(reposToRemove);
            reposRemoved = reposToRemove.Count;

            await _db.SaveChangesAsync();

            // ---- Step 2: Fetch all images from AWS in parallel (no DB access) ----
            var allRepoRecords = await _db.EcrRepositoryRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            var fetchTasks = allRepoRecords.Select(async repo =>
            {
                var images = await provider.FetchAllImages(account, repo.RepositoryName);
                return (repo, images);
            }).ToList();

            var fetchResults = await Task.WhenAll(fetchTasks);

            // ---- Step 3: Sync images sequentially (DbContext is not thread-safe) ----
            var repoSyncResults = new List<(EcrRepositoryRecord repo, int imgAdded, int imgUpdated, int imgRemoved)>();

            foreach (var (repo, cloudImages) in fetchResults)
            {
                var dbImages = await _db.EcrImageRecords
                    .Where(x => x.RepositoryRecordId == repo.Id && x.CloudAccountId == accountId)
                    .ToListAsync();

                int imgAdded = 0, imgUpdated = 0, imgRemoved = 0;

                foreach (var cloud in cloudImages)
                {
                    var existing = dbImages.FirstOrDefault(i => i.ImageDigest == cloud.ImageDigest);

                    if (existing is null)
                    {
                        _db.EcrImageRecords.Add(new EcrImageRecord
                        {
                            RepositoryRecordId = repo.Id,
                            RepositoryName = repo.RepositoryName,
                            ImageTag = cloud.ImageTag,
                            ImageDigest = cloud.ImageDigest,
                            SizeInBytes = cloud.SizeInBytes,
                            ScanStatus = cloud.ScanStatus,
                            FindingsCritical = cloud.FindingsCritical,
                            FindingsHigh = cloud.FindingsHigh,
                            FindingsMedium = cloud.FindingsMedium,
                            FindingsLow = cloud.FindingsLow,
                            Provider = account.Provider!,
                            CloudAccountId = accountId,
                            CreatedBy = user,
                            PushedAt = cloud.PushedAt,
                            CreatedAt = DateTime.UtcNow
                        });
                        imgAdded++;
                    }
                    else
                    {
                        bool changed =
                            existing.ScanStatus != cloud.ScanStatus ||
                            existing.FindingsCritical != cloud.FindingsCritical ||
                            existing.FindingsHigh != cloud.FindingsHigh ||
                            existing.ImageTag != cloud.ImageTag;

                        if (changed)
                        {
                            existing.ScanStatus = cloud.ScanStatus;
                            existing.FindingsCritical = cloud.FindingsCritical;
                            existing.FindingsHigh = cloud.FindingsHigh;
                            existing.FindingsMedium = cloud.FindingsMedium;
                            existing.FindingsLow = cloud.FindingsLow;
                            existing.ImageTag = cloud.ImageTag;
                            existing.UpdatedAt = DateTime.UtcNow;
                            imgUpdated++;
                        }
                    }
                }

                var cloudDigests = cloudImages.Select(i => i.ImageDigest).ToHashSet();
                var toRemove = dbImages.Where(i => string.IsNullOrEmpty(i.ImageDigest) || !cloudDigests.Contains(i.ImageDigest)).ToList();
                _db.EcrImageRecords.RemoveRange(toRemove);
                imgRemoved = toRemove.Count;

                repoSyncResults.Add((repo, imgAdded, imgUpdated, imgRemoved));
            }

            await _db.SaveChangesAsync();

            // ---- Build final response ----
            var finalRepoSyncResults = new List<RepositoryImageSyncResult>();

            foreach (var (repo, imgAdded, imgUpdated, imgRemoved) in repoSyncResults)
            {
                var finalImages = await _db.EcrImageRecords
                    .Where(x => x.RepositoryRecordId == repo.Id && x.CloudAccountId == accountId)
                    .OrderByDescending(x => x.PushedAt)
                    .ToListAsync();

                finalRepoSyncResults.Add(new RepositoryImageSyncResult
                {
                    Repository = MapRepo(repo),
                    ImagesAdded = imgAdded,
                    ImagesUpdated = imgUpdated,
                    ImagesRemoved = imgRemoved,
                    Images = finalImages.Select(MapImage).ToList()
                });
            }

            return new FullEcrSyncResult
            {
                RepositoriesAdded = reposAdded,
                RepositoriesUpdated = reposUpdated,
                RepositoriesRemoved = reposRemoved,
                Repositories = finalRepoSyncResults
            };
        }
    }
}
