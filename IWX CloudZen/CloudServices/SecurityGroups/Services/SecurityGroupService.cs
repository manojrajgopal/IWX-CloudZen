using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.SecurityGroups.DTOs;
using IWX_CloudZen.CloudServices.SecurityGroups.Entities;
using IWX_CloudZen.CloudServices.SecurityGroups.Factory;
using IWX_CloudZen.CloudServices.SecurityGroups.Interfaces;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IWX_CloudZen.CloudServices.SecurityGroups.Services
{
    public class SecurityGroupService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public SecurityGroupService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ================================================================
        // Resolve credentials + provider
        // ================================================================

        private async Task<(CloudAccounts.DTOs.CloudConnectionSecrets account, ISecurityGroupProvider provider)>
            Resolve(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = SecurityGroupProviderFactory.Get(
                account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            return (account, provider);
        }

        // ================================================================
        // Mapper
        // ================================================================

        private static SecurityGroupResponse Map(SecurityGroupRecord r) => new()
        {
            Id = r.Id,
            SecurityGroupId = r.SecurityGroupId,
            GroupName = r.GroupName,
            Description = r.Description,
            VpcId = r.VpcId,
            OwnerId = r.OwnerId,
            InboundRules = string.IsNullOrEmpty(r.InboundRulesJson)
                ? new()
                : JsonSerializer.Deserialize<List<SecurityGroupRuleDto>>(r.InboundRulesJson) ?? new(),
            OutboundRules = string.IsNullOrEmpty(r.OutboundRulesJson)
                ? new()
                : JsonSerializer.Deserialize<List<SecurityGroupRuleDto>>(r.OutboundRulesJson) ?? new(),
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static void ApplyCloudInfo(SecurityGroupRecord record, CloudSecurityGroupInfo cloud)
        {
            record.SecurityGroupId = cloud.SecurityGroupId;
            record.GroupName = cloud.GroupName;
            record.Description = cloud.Description;
            record.VpcId = cloud.VpcId;
            record.OwnerId = cloud.OwnerId;
            record.InboundRulesJson = JsonSerializer.Serialize(cloud.InboundRules);
            record.OutboundRulesJson = JsonSerializer.Serialize(cloud.OutboundRules);
        }

        // ================================================================
        // LIST
        // ================================================================

        public async Task<SecurityGroupListResponse> ListSecurityGroups(
            string user, int accountId, string? vpcId = null)
        {
            var query = _db.SecurityGroupRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(vpcId))
                query = query.Where(x => x.VpcId == vpcId);

            var records = await query
                .OrderBy(x => x.GroupName)
                .ToListAsync();

            return new SecurityGroupListResponse
            {
                TotalCount = records.Count,
                VpcIdFilter = vpcId,
                SecurityGroups = records.Select(Map).ToList()
            };
        }

        // ================================================================
        // GET
        // ================================================================

        public async Task<SecurityGroupResponse> GetSecurityGroup(
            string user, int accountId, int id)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            return Map(record);
        }

        // ================================================================
        // CREATE
        // ================================================================

        public async Task<SecurityGroupResponse> CreateSecurityGroup(
            string user, int accountId, CreateSecurityGroupRequest request)
        {
            // Duplicate guard — prevent double-create if same name+VPC already tracked
            var existing = await _db.SecurityGroupRecords.FirstOrDefaultAsync(x =>
                x.CloudAccountId == accountId &&
                x.CreatedBy == user &&
                x.GroupName == request.GroupName &&
                x.VpcId == request.VpcId);

            if (existing is not null)
                throw new InvalidOperationException(
                    $"A security group named '{request.GroupName}' in VPC '{request.VpcId}' is already tracked (DB id={existing.Id}, AWS id={existing.SecurityGroupId}). Use Sync to refresh it.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.CreateSecurityGroup(account, request);

            // Sanity check — provider must return the AWS group ID
            if (string.IsNullOrWhiteSpace(cloudInfo.SecurityGroupId))
                throw new InvalidOperationException(
                    "AWS returned an empty security group ID after creation. The group may still have been created on AWS — run Sync to reconcile before retrying.");

            var record = new SecurityGroupRecord
            {
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            ApplyCloudInfo(record, cloudInfo);
            _db.SecurityGroupRecords.Add(record);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ================================================================
        // UPDATE (Name tag)
        // ================================================================

        public async Task<SecurityGroupResponse> UpdateSecurityGroup(
            string user, int accountId, int id, UpdateSecurityGroupRequest request)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.UpdateSecurityGroup(account, record.SecurityGroupId, request);

            ApplyCloudInfo(record, cloudInfo);

            // ApplyCloudInfo re-maps GroupName from the AWS Name tag (via provider).
            // If the provider re-describe races before the tag propagates, enforce it directly from the request.
            if (!string.IsNullOrWhiteSpace(request.Name))
                record.GroupName = request.Name;

            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ================================================================
        // DELETE
        // ================================================================

        public async Task DeleteSecurityGroup(string user, int accountId, int id)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            var (account, provider) = await Resolve(user, accountId);

            await provider.DeleteSecurityGroup(account, record.SecurityGroupId);

            _db.SecurityGroupRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // INBOUND RULES
        // ================================================================

        public async Task<SecurityGroupResponse> AddInboundRules(
            string user, int accountId, int id, List<SecurityGroupRuleDto> rules)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.AddInboundRules(account, record.SecurityGroupId, rules);

            ApplyCloudInfo(record, cloudInfo);
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task<SecurityGroupResponse> RemoveInboundRules(
            string user, int accountId, int id, List<string> ruleIds)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.RemoveInboundRules(account, record.SecurityGroupId, ruleIds);

            ApplyCloudInfo(record, cloudInfo);
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ================================================================
        // OUTBOUND RULES
        // ================================================================

        public async Task<SecurityGroupResponse> AddOutboundRules(
            string user, int accountId, int id, List<SecurityGroupRuleDto> rules)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.AddOutboundRules(account, record.SecurityGroupId, rules);

            ApplyCloudInfo(record, cloudInfo);
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task<SecurityGroupResponse> RemoveOutboundRules(
            string user, int accountId, int id, List<string> ruleIds)
        {
            var record = await _db.SecurityGroupRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Security group not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.RemoveOutboundRules(account, record.SecurityGroupId, ruleIds);

            ApplyCloudInfo(record, cloudInfo);
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Map(record);
        }

        // ================================================================
        // SYNC
        // ================================================================

        public async Task<SyncSecurityGroupResult> SyncSecurityGroups(
            string user, int accountId, string? vpcId = null)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudGroups = await provider.FetchAllSecurityGroups(account, vpcId);

            var query = _db.SecurityGroupRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(vpcId))
                query = query.Where(x => x.VpcId == vpcId);

            var dbGroups = await query.ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudGroups)
            {
                var existing = dbGroups.FirstOrDefault(g => g.SecurityGroupId == cloud.SecurityGroupId);

                if (existing is null)
                {
                    var newRecord = new SecurityGroupRecord
                    {
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    };
                    ApplyCloudInfo(newRecord, cloud);
                    _db.SecurityGroupRecords.Add(newRecord);
                    added++;
                }
                else
                {
                    var inboundJson = JsonSerializer.Serialize(cloud.InboundRules);
                    var outboundJson = JsonSerializer.Serialize(cloud.OutboundRules);

                    bool changed =
                        existing.GroupName != cloud.GroupName ||
                        existing.Description != cloud.Description ||
                        existing.InboundRulesJson != inboundJson ||
                        existing.OutboundRulesJson != outboundJson;

                    if (changed)
                    {
                        ApplyCloudInfo(existing, cloud);
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // Remove DB records no longer in the cloud
            var cloudIds = cloudGroups.Select(g => g.SecurityGroupId).ToHashSet();
            var toRemove = dbGroups.Where(g => !cloudIds.Contains(g.SecurityGroupId)).ToList();
            _db.SecurityGroupRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var reloadQuery = _db.SecurityGroupRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(vpcId))
                reloadQuery = reloadQuery.Where(x => x.VpcId == vpcId);

            var finalRecords = await reloadQuery
                .OrderBy(x => x.GroupName)
                .ToListAsync();

            return new SyncSecurityGroupResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                SecurityGroups = finalRecords.Select(Map).ToList()
            };
        }
    }
}
