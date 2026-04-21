using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.InternetGateway.DTOs;
using IWX_CloudZen.CloudServices.InternetGateway.Entities;
using IWX_CloudZen.CloudServices.InternetGateway.Factory;
using IWX_CloudZen.Data;
using IWX_CloudZen.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.InternetGateway.Services
{
    public class InternetGatewayService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public InternetGatewayService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        private static InternetGatewayResponse Map(InternetGatewayRecord r) => new()
        {
            Id = r.Id,
            InternetGatewayId = r.InternetGatewayId,
            Name = r.Name,
            AttachedVpcId = r.AttachedVpcId,
            State = r.State,
            OwnerId = r.OwnerId,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        // ---- List ----

        public async Task<InternetGatewayListResponse> ListInternetGateways(string user, int accountId)
        {
            var records = await _db.InternetGatewayRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            return new InternetGatewayListResponse { InternetGateways = records.Select(Map).ToList() };
        }

        // ---- Get by ID ----

        public async Task<InternetGatewayResponse> GetInternetGateway(string user, int accountId, int id)
        {
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Internet Gateway not found.");

            return Map(record);
        }

        // ---- Get by VPC ----

        public async Task<InternetGatewayResponse?> GetInternetGatewayByVpc(string user, int accountId, string vpcId)
        {
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.AttachedVpcId == vpcId && x.CloudAccountId == accountId && x.CreatedBy == user);

            return record is null ? null : Map(record);
        }

        // ---- Create ----

        public async Task<InternetGatewayResponse> CreateInternetGateway(string user, int accountId, CreateInternetGatewayRequest request)
        {
            var normalizedName = CloudResourceNameNormalizer.NormalizeGeneralName(request.Name);

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.CreateInternetGateway(account, normalizedName, request.VpcId);

            var record = new InternetGatewayRecord
            {
                InternetGatewayId = info.InternetGatewayId,
                Name = normalizedName,
                AttachedVpcId = info.AttachedVpcId,
                State = info.State,
                OwnerId = info.OwnerId,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.InternetGatewayRecords.Add(record);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ---- Update (rename) ----

        public async Task<InternetGatewayResponse> UpdateInternetGateway(string user, int accountId, int id, UpdateInternetGatewayRequest request)
        {
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Internet Gateway not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.UpdateInternetGateway(account, record.InternetGatewayId, request.Name);

            if (!string.IsNullOrWhiteSpace(info.Name)) record.Name = info.Name;
            record.State = info.State;
            record.AttachedVpcId = info.AttachedVpcId;
            record.OwnerId = info.OwnerId;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ---- Delete ----

        public async Task DeleteInternetGateway(string user, int accountId, int id)
        {
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Internet Gateway not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteInternetGateway(account, record.InternetGatewayId);

            _db.InternetGatewayRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ---- Attach to VPC ----

        public async Task<InternetGatewayResponse> AttachToVpc(string user, int accountId, int id, AttachInternetGatewayRequest request)
        {
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Internet Gateway not found.");

            if (!string.IsNullOrEmpty(record.AttachedVpcId))
                throw new InvalidOperationException($"Internet Gateway is already attached to VPC '{record.AttachedVpcId}'. Detach it first.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.AttachToVpc(account, record.InternetGatewayId, request.VpcId);

            record.AttachedVpcId = info.AttachedVpcId;
            record.State = info.State;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ---- Detach from VPC ----

        public async Task<InternetGatewayResponse> DetachFromVpc(string user, int accountId, int id, DetachInternetGatewayRequest request)
        {
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Internet Gateway not found.");

            if (string.IsNullOrEmpty(record.AttachedVpcId))
                throw new InvalidOperationException("Internet Gateway is not attached to any VPC.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DetachFromVpc(account, record.InternetGatewayId, request.VpcId);

            record.AttachedVpcId = null;
            record.State = "detached";
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ---- Get IGW for VPC (live from cloud) ----

        public async Task<InternetGatewayResponse?> GetInternetGatewayForVpc(string user, int accountId, string vpcId)
        {
            // First check DB
            var record = await _db.InternetGatewayRecords
                .FirstOrDefaultAsync(x => x.AttachedVpcId == vpcId && x.CloudAccountId == accountId && x.CreatedBy == user);

            if (record is not null) return Map(record);

            // If not in DB, check cloud directly
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.GetInternetGatewayForVpc(account, vpcId);
            if (info is null) return null;

            // Save to DB since we found it in the cloud
            var newRecord = new InternetGatewayRecord
            {
                InternetGatewayId = info.InternetGatewayId,
                Name = info.Name,
                AttachedVpcId = info.AttachedVpcId,
                State = info.State,
                OwnerId = info.OwnerId,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.InternetGatewayRecords.Add(newRecord);
            await _db.SaveChangesAsync();

            return Map(newRecord);
        }

        // ---- Sync ----

        public async Task<SyncInternetGatewayResult> SyncInternetGateways(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = InternetGatewayProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudIgws = await provider.FetchAllInternetGateways(account);

            var dbIgws = await _db.InternetGatewayRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudIgws)
            {
                var existing = dbIgws.FirstOrDefault(r => r.InternetGatewayId == cloud.InternetGatewayId);
                if (existing is null)
                {
                    _db.InternetGatewayRecords.Add(new InternetGatewayRecord
                    {
                        InternetGatewayId = cloud.InternetGatewayId,
                        Name = cloud.Name,
                        AttachedVpcId = cloud.AttachedVpcId,
                        State = cloud.State,
                        OwnerId = cloud.OwnerId,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    bool changed =
                        existing.Name != cloud.Name ||
                        existing.AttachedVpcId != cloud.AttachedVpcId ||
                        existing.State != cloud.State ||
                        existing.OwnerId != cloud.OwnerId;

                    if (changed)
                    {
                        existing.Name = cloud.Name;
                        existing.AttachedVpcId = cloud.AttachedVpcId;
                        existing.State = cloud.State;
                        existing.OwnerId = cloud.OwnerId;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudIds = cloudIgws.Select(v => v.InternetGatewayId).ToHashSet();
            var toRemove = dbIgws.Where(r => !cloudIds.Contains(r.InternetGatewayId)).ToList();
            _db.InternetGatewayRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.InternetGatewayRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            return new SyncInternetGatewayResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                InternetGateways = finalRecords.Select(Map).ToList()
            };
        }
    }
}
