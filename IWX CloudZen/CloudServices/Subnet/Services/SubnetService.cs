using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.Subnet.DTOs;
using IWX_CloudZen.CloudServices.Subnet.Entities;
using IWX_CloudZen.CloudServices.Subnet.Factory;
using IWX_CloudZen.CloudServices.Subnet.Interfaces;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.Subnet.Services
{
    public class SubnetService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public SubnetService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ================================================================
        // Resolve credentials + provider
        // ================================================================

        private async Task<(CloudAccounts.DTOs.CloudConnectionSecrets account, ISubnetProvider provider)>
            Resolve(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = SubnetProviderFactory.Get(
                account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            return (account, provider);
        }

        // ================================================================
        // Mapper
        // ================================================================

        private static SubnetResponse Map(SubnetRecord r) => new()
        {
            Id = r.Id,
            SubnetId = r.SubnetId,
            Name = r.Name,
            VpcId = r.VpcId,
            CidrBlock = r.CidrBlock,
            Ipv6CidrBlock = r.Ipv6CidrBlock,
            AvailabilityZone = r.AvailabilityZone,
            AvailabilityZoneId = r.AvailabilityZoneId,
            State = r.State,
            AvailableIpAddressCount = r.AvailableIpAddressCount,
            IsDefault = r.IsDefault,
            MapPublicIpOnLaunch = r.MapPublicIpOnLaunch,
            AssignIpv6AddressOnCreation = r.AssignIpv6AddressOnCreation,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        // ================================================================
        // LIST
        // ================================================================

        public async Task<SubnetListResponse> ListSubnets(
            string user, int accountId, string? vpcId = null)
        {
            var query = _db.SubnetRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(vpcId))
                query = query.Where(x => x.VpcId == vpcId);

            var records = await query
                .OrderBy(x => x.AvailabilityZone)
                .ThenBy(x => x.CidrBlock)
                .ToListAsync();

            return new SubnetListResponse
            {
                TotalCount = records.Count,
                VpcIdFilter = vpcId,
                Subnets = records.Select(Map).ToList()
            };
        }

        // ================================================================
        // GET
        // ================================================================

        public async Task<SubnetResponse> GetSubnet(string user, int accountId, int id)
        {
            var record = await _db.SubnetRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Subnet not found.");

            return Map(record);
        }

        // ================================================================
        // CREATE
        // ================================================================

        public async Task<SubnetResponse> CreateSubnet(
            string user, int accountId, CreateSubnetRequest request)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.CreateSubnet(account, request);

            var record = new SubnetRecord
            {
                SubnetId = cloudInfo.SubnetId,
                Name = cloudInfo.Name,
                VpcId = cloudInfo.VpcId,
                CidrBlock = cloudInfo.CidrBlock,
                Ipv6CidrBlock = cloudInfo.Ipv6CidrBlock,
                AvailabilityZone = cloudInfo.AvailabilityZone,
                AvailabilityZoneId = cloudInfo.AvailabilityZoneId,
                State = cloudInfo.State,
                AvailableIpAddressCount = cloudInfo.AvailableIpAddressCount,
                IsDefault = cloudInfo.IsDefault,
                MapPublicIpOnLaunch = cloudInfo.MapPublicIpOnLaunch,
                AssignIpv6AddressOnCreation = cloudInfo.AssignIpv6AddressOnCreation,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.SubnetRecords.Add(record);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ================================================================
        // UPDATE
        // ================================================================

        public async Task<SubnetResponse> UpdateSubnet(
            string user, int accountId, int id, UpdateSubnetRequest request)
        {
            var record = await _db.SubnetRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Subnet not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.UpdateSubnet(account, record.SubnetId, request);

            // Reflect all mutable fields returned from AWS
            record.Name = cloudInfo.Name;
            record.State = cloudInfo.State;
            record.AvailableIpAddressCount = cloudInfo.AvailableIpAddressCount;
            record.MapPublicIpOnLaunch = cloudInfo.MapPublicIpOnLaunch;
            record.AssignIpv6AddressOnCreation = cloudInfo.AssignIpv6AddressOnCreation;
            record.Ipv6CidrBlock = cloudInfo.Ipv6CidrBlock;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ================================================================
        // DELETE
        // ================================================================

        public async Task DeleteSubnet(string user, int accountId, int id)
        {
            var record = await _db.SubnetRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Subnet not found.");

            var (account, provider) = await Resolve(user, accountId);

            await provider.DeleteSubnet(account, record.SubnetId);

            _db.SubnetRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // SYNC
        // ================================================================

        public async Task<SyncSubnetResult> SyncSubnets(
            string user, int accountId, string? vpcId = null)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudSubnets = await provider.FetchAllSubnets(account, vpcId);

            var query = _db.SubnetRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(vpcId))
                query = query.Where(x => x.VpcId == vpcId);

            var dbSubnets = await query.ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudSubnets)
            {
                var existing = dbSubnets.FirstOrDefault(s => s.SubnetId == cloud.SubnetId);

                if (existing is null)
                {
                    _db.SubnetRecords.Add(new SubnetRecord
                    {
                        SubnetId = cloud.SubnetId,
                        Name = cloud.Name,
                        VpcId = cloud.VpcId,
                        CidrBlock = cloud.CidrBlock,
                        Ipv6CidrBlock = cloud.Ipv6CidrBlock,
                        AvailabilityZone = cloud.AvailabilityZone,
                        AvailabilityZoneId = cloud.AvailabilityZoneId,
                        State = cloud.State,
                        AvailableIpAddressCount = cloud.AvailableIpAddressCount,
                        IsDefault = cloud.IsDefault,
                        MapPublicIpOnLaunch = cloud.MapPublicIpOnLaunch,
                        AssignIpv6AddressOnCreation = cloud.AssignIpv6AddressOnCreation,
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
                        existing.AvailableIpAddressCount != cloud.AvailableIpAddressCount ||
                        existing.MapPublicIpOnLaunch != cloud.MapPublicIpOnLaunch ||
                        existing.AssignIpv6AddressOnCreation != cloud.AssignIpv6AddressOnCreation ||
                        existing.Ipv6CidrBlock != cloud.Ipv6CidrBlock;

                    if (changed)
                    {
                        existing.Name = cloud.Name;
                        existing.State = cloud.State;
                        existing.AvailableIpAddressCount = cloud.AvailableIpAddressCount;
                        existing.MapPublicIpOnLaunch = cloud.MapPublicIpOnLaunch;
                        existing.AssignIpv6AddressOnCreation = cloud.AssignIpv6AddressOnCreation;
                        existing.Ipv6CidrBlock = cloud.Ipv6CidrBlock;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // Remove DB records no longer in the cloud
            var cloudIds = cloudSubnets.Select(s => s.SubnetId).ToHashSet();
            var toRemove = dbSubnets.Where(s => !cloudIds.Contains(s.SubnetId)).ToList();
            _db.SubnetRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            // Return the final state
            var reloadQuery = _db.SubnetRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(vpcId))
                reloadQuery = reloadQuery.Where(x => x.VpcId == vpcId);

            var finalRecords = await reloadQuery
                .OrderBy(x => x.AvailabilityZone)
                .ThenBy(x => x.CidrBlock)
                .ToListAsync();

            return new SyncSubnetResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Subnets = finalRecords.Select(Map).ToList()
            };
        }
    }
}
