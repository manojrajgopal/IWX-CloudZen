using System.Text.Json;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.KeyPair.DTOs;
using IWX_CloudZen.CloudServices.KeyPair.Entities;
using IWX_CloudZen.CloudServices.KeyPair.Factory;
using IWX_CloudZen.Data;
using IWX_CloudZen.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.KeyPair.Services
{
    public class KeyPairService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public KeyPairService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ---- Helpers / Mappers ----

        private static KeyPairResponse Map(KeyPairRecord r) => new()
        {
            Id                = r.Id,
            KeyPairId         = r.KeyPairId,
            KeyName           = r.KeyName,
            KeyFingerprint    = r.KeyFingerprint,
            KeyType           = r.KeyType,
            IsImported        = r.IsImported,
            HasPrivateKey     = !string.IsNullOrWhiteSpace(r.PrivateKeyMaterial),
            PublicKeyMaterial = r.PublicKeyMaterial ?? string.Empty,
            PrivateKeyMaterial = r.PrivateKeyMaterial ?? string.Empty,
            Tags              = DeserializeTags(r.TagsJson),
            AwsCreatedAt      = r.AwsCreatedAt,
            Provider          = r.Provider,
            CloudAccountId    = r.CloudAccountId,
            CreatedAt         = r.CreatedAt,
            UpdatedAt         = r.UpdatedAt
        };

        private static KeyPairCreatedResponse MapCreated(KeyPairRecord r) => new()
        {
            Id                 = r.Id,
            KeyPairId          = r.KeyPairId,
            KeyName            = r.KeyName,
            KeyFingerprint     = r.KeyFingerprint,
            KeyType            = r.KeyType,
            IsImported         = r.IsImported,
            HasPrivateKey      = !string.IsNullOrWhiteSpace(r.PrivateKeyMaterial),
            PublicKeyMaterial  = r.PublicKeyMaterial ?? string.Empty,
            PrivateKeyMaterial = r.PrivateKeyMaterial ?? string.Empty,
            Tags               = DeserializeTags(r.TagsJson),
            AwsCreatedAt       = r.AwsCreatedAt,
            Provider           = r.Provider,
            CloudAccountId     = r.CloudAccountId,
            CreatedAt          = r.CreatedAt,
            UpdatedAt          = r.UpdatedAt
        };

        private static string? SerializeTags(Dictionary<string, string> tags) =>
            tags.Count > 0 ? JsonSerializer.Serialize(tags) : null;

        private static Dictionary<string, string> DeserializeTags(string? json) =>
            string.IsNullOrWhiteSpace(json)
                ? new()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

        private static bool HasChanges(KeyPairRecord record, CloudKeyPairInfo cloud) =>
            record.KeyFingerprint != cloud.KeyFingerprint ||
            record.KeyType != cloud.KeyType ||
            (record.PublicKeyMaterial ?? string.Empty) != cloud.PublicKeyMaterial;

        private async Task<(IWX_CloudZen.CloudAccounts.DTOs.CloudConnectionSecrets account,
                            IWX_CloudZen.CloudServices.KeyPair.Interfaces.IKeyPairProvider provider)>
            ResolveAsync(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = KeyPairProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return (account, provider);
        }

        // ---- CRUD Operations ----

        public async Task<KeyPairListResponse> ListKeyPairs(string user, int accountId)
        {
            var records = await _db.KeyPairRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderBy(x => x.KeyName)
                .ToListAsync();

            return new KeyPairListResponse { KeyPairs = records.Select(Map).ToList() };
        }

        public async Task<KeyPairResponse> GetKeyPair(string user, int accountId, int keyPairDbId)
        {
            var record = await _db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.Id == keyPairDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Key pair not found.");

            return Map(record);
        }

        public async Task<KeyPairCreatedResponse> CreateKeyPair(string user, int accountId, CreateKeyPairRequest request)
        {
            var (account, provider) = await ResolveAsync(user, accountId);

            request.KeyName = CloudResourceNameNormalizer.NormalizeGeneralName(request.KeyName);

            // Prevent duplicate names within the same account
            bool exists = await _db.KeyPairRecords
                .AnyAsync(x => x.CloudAccountId == accountId && x.KeyName == request.KeyName);
            if (exists)
                throw new InvalidOperationException($"Key pair '{request.KeyName}' already exists in this account.");

            var info = await provider.CreateKeyPair(
                account,
                request.KeyName,
                request.KeyType,
                request.Tags.Count > 0 ? request.Tags : null);

            var record = new KeyPairRecord
            {
                KeyPairId          = info.KeyPairId,
                KeyName            = info.KeyName,
                KeyFingerprint     = info.KeyFingerprint,
                KeyType            = info.KeyType,
                PrivateKeyMaterial = info.PrivateKeyMaterial,
                PublicKeyMaterial  = info.PublicKeyMaterial,
                IsImported         = false,
                TagsJson           = SerializeTags(info.Tags),
                AwsCreatedAt       = info.AwsCreatedAt,
                Provider           = account.Provider!,
                CloudAccountId     = accountId,
                CreatedBy          = user,
                CreatedAt          = DateTime.UtcNow
            };

            _db.KeyPairRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapCreated(record);
        }

        public async Task<KeyPairResponse> ImportKeyPair(string user, int accountId, ImportKeyPairRequest request)
        {
            var (account, provider) = await ResolveAsync(user, accountId);

            bool exists = await _db.KeyPairRecords
                .AnyAsync(x => x.CloudAccountId == accountId && x.KeyName == request.KeyName);
            if (exists)
                throw new InvalidOperationException($"Key pair '{request.KeyName}' already exists in this account.");

            var info = await provider.ImportKeyPair(
                account,
                request.KeyName,
                request.PublicKeyMaterial,
                request.Tags.Count > 0 ? request.Tags : null);

            var record = new KeyPairRecord
            {
                KeyPairId          = info.KeyPairId,
                KeyName            = info.KeyName,
                KeyFingerprint     = info.KeyFingerprint,
                KeyType            = info.KeyType,
                PrivateKeyMaterial = null,
                PublicKeyMaterial  = request.PublicKeyMaterial,
                IsImported         = true,
                TagsJson           = SerializeTags(info.Tags),
                AwsCreatedAt       = info.AwsCreatedAt,
                Provider           = account.Provider!,
                CloudAccountId     = accountId,
                CreatedBy          = user,
                CreatedAt          = DateTime.UtcNow
            };

            _db.KeyPairRecords.Add(record);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task<KeyPairResponse> UpdateKeyPairTags(string user, int accountId, int keyPairDbId, UpdateKeyPairRequest request)
        {
            var record = await _db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.Id == keyPairDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Key pair not found.");

            var (account, provider) = await ResolveAsync(user, accountId);

            await provider.UpdateKeyPairTags(account, record.KeyName, request.Tags);

            // Merge tags: existing tags + new/updated tags
            var currentTags = DeserializeTags(record.TagsJson);
            foreach (var kvp in request.Tags)
                currentTags[kvp.Key] = kvp.Value;

            record.TagsJson   = SerializeTags(currentTags);
            record.UpdatedAt  = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task DeleteKeyPair(string user, int accountId, int keyPairDbId)
        {
            var record = await _db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.Id == keyPairDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Key pair not found.");

            var (account, provider) = await ResolveAsync(user, accountId);

            await provider.DeleteKeyPair(account, record.KeyName);

            _db.KeyPairRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves the stored private key PEM from the database.
        /// Only available for key pairs that were created through this API (not imported or synced).
        /// </summary>
        public async Task<KeyPairPrivateKeyResponse> DownloadPrivateKey(string user, int accountId, int keyPairDbId)
        {
            var record = await _db.KeyPairRecords
                .FirstOrDefaultAsync(x => x.Id == keyPairDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Key pair not found.");

            if (string.IsNullOrWhiteSpace(record.PrivateKeyMaterial))
                throw new InvalidOperationException(
                    "Private key is not stored for this key pair. " +
                    "It is only available for key pairs that were created through this API.");

            return new KeyPairPrivateKeyResponse
            {
                Id                 = record.Id,
                KeyName            = record.KeyName,
                PrivateKeyMaterial = record.PrivateKeyMaterial
            };
        }

        // ---- Sync ----

        public async Task<SyncKeyPairsResult> SyncKeyPairs(string user, int accountId)
        {
            var (account, provider) = await ResolveAsync(user, accountId);

            var cloudKeyPairs = await provider.FetchAllKeyPairs(account);

            var dbRecords = await _db.KeyPairRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudKeyPairs)
            {
                var existing = dbRecords.FirstOrDefault(r => r.KeyPairId == cloud.KeyPairId)
                            ?? dbRecords.FirstOrDefault(r => r.KeyName == cloud.KeyName);

                if (existing is null)
                {
                    _db.KeyPairRecords.Add(new KeyPairRecord
                    {
                        KeyPairId          = cloud.KeyPairId,
                        KeyName            = cloud.KeyName,
                        KeyFingerprint     = cloud.KeyFingerprint,
                        KeyType            = cloud.KeyType,
                        PrivateKeyMaterial = null,   // never available during sync
                        PublicKeyMaterial  = cloud.PublicKeyMaterial,
                        IsImported         = false,  // unknown origin during sync
                        TagsJson           = SerializeTags(cloud.Tags),
                        AwsCreatedAt       = cloud.AwsCreatedAt,
                        Provider           = account.Provider!,
                        CloudAccountId     = accountId,
                        CreatedBy          = user,
                        CreatedAt          = DateTime.UtcNow
                    });
                    added++;
                }
                else if (HasChanges(existing, cloud))
                {
                    existing.KeyPairId         = cloud.KeyPairId;
                    existing.KeyName           = cloud.KeyName;
                    existing.KeyFingerprint    = cloud.KeyFingerprint;
                    existing.KeyType           = cloud.KeyType;
                    existing.PublicKeyMaterial  = cloud.PublicKeyMaterial;
                    existing.TagsJson          = SerializeTags(cloud.Tags);
                    existing.AwsCreatedAt   = cloud.AwsCreatedAt;
                    existing.UpdatedAt      = DateTime.UtcNow;
                    updated++;
                }
            }

            var cloudIds = cloudKeyPairs.Select(kp => kp.KeyPairId).ToHashSet();
            var toRemove = dbRecords.Where(r => !cloudIds.Contains(r.KeyPairId)).ToList();
            _db.KeyPairRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.KeyPairRecords
                .Where(x => x.CloudAccountId == accountId)
                .OrderBy(x => x.KeyName)
                .ToListAsync();

            return new SyncKeyPairsResult
            {
                Added    = added,
                Updated  = updated,
                Removed  = removed,
                KeyPairs = finalRecords.Select(Map).ToList()
            };
        }
    }
}
