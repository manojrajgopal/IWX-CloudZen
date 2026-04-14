using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.VPC.DTOs;
using IWX_CloudZen.CloudServices.VPC.Entities;
using IWX_CloudZen.CloudServices.VPC.Factory;
using IWX_CloudZen.Data;
using IWX_CloudZen.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.VPC.Services
{
    public class VpcService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public VpcService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        private static VpcResponse Map(VpcRecord r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            VpcId = r.VpcId,
            CidrBlock = r.CidrBlock,
            State = r.State,
            IsDefault = r.IsDefault,
            EnableDnsSupport = r.EnableDnsSupport,
            EnableDnsHostnames = r.EnableDnsHostnames,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        public async Task<VpcListResponse> ListVpcs(string user, int accountId)
        {
            var records = await _db.VpcRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            return new VpcListResponse { Vpcs = records.Select(Map).ToList() };
        }

        public async Task<VpcResponse> CreateVpc(string user, int accountId, CreateVpcRequest request)
        {
            var normalizedVpcName = CloudResourceNameNormalizer.NormalizeGeneralName(request.VpcName);

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = VpcProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.CreateVpc(
                account,
                normalizedVpcName,
                request.CidrBlock,
                request.EnableDnsSupport,
                request.EnableDnsHostnames);

            var record = new VpcRecord
            {
                Name = normalizedVpcName,
                VpcId = info.VpcId,
                CidrBlock = info.CidrBlock,
                State = info.State,
                IsDefault = info.IsDefault,
                EnableDnsSupport = info.EnableDnsSupport,
                EnableDnsHostnames = info.EnableDnsHostnames,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.VpcRecords.Add(record);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task<VpcResponse> UpdateVpc(string user, int accountId, int vpcDbId, UpdateVpcRequest request)
        {
            var record = await _db.VpcRecords
                .FirstOrDefaultAsync(x => x.Id == vpcDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("VPC not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = VpcProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.UpdateVpc(
                account,
                record.VpcId,
                request.VpcName,
                request.EnableDnsSupport,
                request.EnableDnsHostnames);

            if (!string.IsNullOrWhiteSpace(info.Name)) record.Name = info.Name;
            record.State = info.State;
            record.EnableDnsSupport = info.EnableDnsSupport;
            record.EnableDnsHostnames = info.EnableDnsHostnames;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task DeleteVpc(string user, int accountId, int vpcDbId)
        {
            var record = await _db.VpcRecords
                .FirstOrDefaultAsync(x => x.Id == vpcDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("VPC not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = VpcProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteVpc(account, record.VpcId);

            _db.VpcRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        public async Task<SyncVpcResult> SyncVpcs(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = VpcProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudVpcs = await provider.FetchAllVpcs(account);

            var dbVpcs = await _db.VpcRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudVpcs)
            {
                var existing = dbVpcs.FirstOrDefault(r => r.VpcId == cloud.VpcId);
                if (existing is null)
                {
                    _db.VpcRecords.Add(new VpcRecord
                    {
                        Name = cloud.Name,
                        VpcId = cloud.VpcId,
                        CidrBlock = cloud.CidrBlock,
                        State = cloud.State,
                        IsDefault = cloud.IsDefault,
                        EnableDnsSupport = cloud.EnableDnsSupport,
                        EnableDnsHostnames = cloud.EnableDnsHostnames,
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
                        existing.State != cloud.State ||
                        existing.EnableDnsSupport != cloud.EnableDnsSupport ||
                        existing.EnableDnsHostnames != cloud.EnableDnsHostnames;

                    if (changed)
                    {
                        existing.Name = cloud.Name;
                        existing.State = cloud.State;
                        existing.EnableDnsSupport = cloud.EnableDnsSupport;
                        existing.EnableDnsHostnames = cloud.EnableDnsHostnames;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudIds = cloudVpcs.Select(v => v.VpcId).ToHashSet();
            var toRemove = dbVpcs.Where(r => !cloudIds.Contains(r.VpcId)).ToList();
            _db.VpcRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.VpcRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            return new SyncVpcResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Vpcs = finalRecords.Select(Map).ToList()
            };
        }
    }
}
