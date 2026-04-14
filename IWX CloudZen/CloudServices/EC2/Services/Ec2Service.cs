using System.Text.Json;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.EC2.DTOs;
using IWX_CloudZen.CloudServices.EC2.Entities;
using IWX_CloudZen.CloudServices.EC2.Factory;
using IWX_CloudZen.Data;
using IWX_CloudZen.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.EC2.Services
{
    public class Ec2Service
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public Ec2Service(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ---- Helpers ----

        private static Ec2InstanceResponse Map(Ec2InstanceRecord r) => new()
        {
            Id = r.Id,
            InstanceId = r.InstanceId,
            Name = r.Name,
            InstanceType = r.InstanceType,
            State = r.State,
            PublicIpAddress = r.PublicIpAddress,
            PrivateIpAddress = r.PrivateIpAddress,
            VpcId = r.VpcId,
            SubnetId = r.SubnetId,
            ImageId = r.ImageId,
            KeyName = r.KeyName,
            Architecture = r.Architecture,
            Platform = r.Platform,
            Monitoring = r.Monitoring,
            EbsOptimized = r.EbsOptimized,
            SecurityGroups = DeserializeSecurityGroups(r.SecurityGroupsJson),
            Tags = DeserializeTags(r.TagsJson),
            LaunchTime = r.LaunchTime,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static string? SerializeSecurityGroups(List<Ec2SecurityGroupDto> groups)
            => groups.Count > 0 ? JsonSerializer.Serialize(groups) : null;

        private static List<Ec2SecurityGroupDto> DeserializeSecurityGroups(string? json)
            => string.IsNullOrWhiteSpace(json)
                ? new()
                : JsonSerializer.Deserialize<List<Ec2SecurityGroupDto>>(json) ?? new();

        private static string? SerializeTags(Dictionary<string, string> tags)
            => tags.Count > 0 ? JsonSerializer.Serialize(tags) : null;

        private static Dictionary<string, string> DeserializeTags(string? json)
            => string.IsNullOrWhiteSpace(json)
                ? new()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

        private Ec2InstanceRecord MapFromCloud(CloudEc2InstanceInfo cloud, string provider, int accountId, string user) => new()
        {
            InstanceId = cloud.InstanceId,
            Name = cloud.Name,
            InstanceType = cloud.InstanceType,
            State = cloud.State,
            PublicIpAddress = cloud.PublicIpAddress,
            PrivateIpAddress = cloud.PrivateIpAddress,
            VpcId = cloud.VpcId,
            SubnetId = cloud.SubnetId,
            ImageId = cloud.ImageId,
            KeyName = cloud.KeyName,
            Architecture = cloud.Architecture,
            Platform = cloud.Platform,
            Monitoring = cloud.Monitoring,
            EbsOptimized = cloud.EbsOptimized,
            SecurityGroupsJson = SerializeSecurityGroups(cloud.SecurityGroups),
            TagsJson = SerializeTags(cloud.Tags),
            LaunchTime = cloud.LaunchTime,
            Provider = provider,
            CloudAccountId = accountId,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };

        private static void UpdateRecordFromCloud(Ec2InstanceRecord record, CloudEc2InstanceInfo cloud)
        {
            record.Name = cloud.Name;
            record.InstanceType = cloud.InstanceType;
            record.State = cloud.State;
            record.PublicIpAddress = cloud.PublicIpAddress;
            record.PrivateIpAddress = cloud.PrivateIpAddress;
            record.VpcId = cloud.VpcId;
            record.SubnetId = cloud.SubnetId;
            record.ImageId = cloud.ImageId;
            record.KeyName = cloud.KeyName;
            record.Architecture = cloud.Architecture;
            record.Platform = cloud.Platform;
            record.Monitoring = cloud.Monitoring;
            record.EbsOptimized = cloud.EbsOptimized;
            record.SecurityGroupsJson = SerializeSecurityGroups(cloud.SecurityGroups);
            record.TagsJson = SerializeTags(cloud.Tags);
            record.LaunchTime = cloud.LaunchTime;
            record.UpdatedAt = DateTime.UtcNow;
        }

        private static bool HasChanges(Ec2InstanceRecord record, CloudEc2InstanceInfo cloud)
        {
            return record.Name != cloud.Name
                || record.InstanceType != cloud.InstanceType
                || record.State != cloud.State
                || record.PublicIpAddress != cloud.PublicIpAddress
                || record.PrivateIpAddress != cloud.PrivateIpAddress
                || record.VpcId != cloud.VpcId
                || record.SubnetId != cloud.SubnetId
                || record.ImageId != cloud.ImageId
                || record.KeyName != cloud.KeyName
                || record.Architecture != cloud.Architecture
                || record.Monitoring != cloud.Monitoring
                || record.EbsOptimized != cloud.EbsOptimized;
        }

        // ---- CRUD Operations ----

        public async Task<Ec2InstanceListResponse> ListInstances(string user, int accountId)
        {
            var records = await _db.Ec2InstanceRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            return new Ec2InstanceListResponse { Instances = records.Select(Map).ToList() };
        }

        public async Task<Ec2InstanceResponse> GetInstance(string user, int accountId, int instanceDbId)
        {
            var record = await _db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == instanceDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            return Map(record);
        }

        public async Task<List<Ec2InstanceResponse>> LaunchInstances(string user, int accountId, LaunchEc2InstanceRequest request)
        {
            var normalizedInstanceName = CloudResourceNameNormalizer.NormalizeGeneralName(request.InstanceName);

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var instances = await provider.LaunchInstances(
                account,
                normalizedInstanceName,
                request.ImageId,
                request.InstanceType,
                request.KeyName,
                request.SubnetId,
                request.SecurityGroupIds,
                request.MinCount,
                request.MaxCount,
                request.EbsOptimized,
                request.UserData,
                request.Tags);

            var records = new List<Ec2InstanceRecord>();
            foreach (var info in instances)
            {
                var record = MapFromCloud(info, account.Provider!, accountId, user);
                _db.Ec2InstanceRecords.Add(record);
                records.Add(record);
            }

            await _db.SaveChangesAsync();

            return records.Select(Map).ToList();
        }

        public async Task<Ec2InstanceResponse> UpdateInstance(string user, int accountId, int instanceDbId, UpdateEc2InstanceRequest request)
        {
            var record = await _db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == instanceDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.UpdateInstance(
                account,
                record.InstanceId,
                request.InstanceName,
                request.InstanceType,
                request.SecurityGroupIds,
                request.Tags);

            UpdateRecordFromCloud(record, info);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task StartInstance(string user, int accountId, int instanceDbId)
        {
            var record = await _db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == instanceDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.StartInstance(account, record.InstanceId);

            record.State = "pending";
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task StopInstance(string user, int accountId, int instanceDbId, bool force = false)
        {
            var record = await _db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == instanceDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.StopInstance(account, record.InstanceId, force);

            record.State = "stopping";
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task RebootInstance(string user, int accountId, int instanceDbId)
        {
            var record = await _db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == instanceDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.RebootInstance(account, record.InstanceId);

            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task TerminateInstance(string user, int accountId, int instanceDbId)
        {
            var record = await _db.Ec2InstanceRecords
                .FirstOrDefaultAsync(x => x.Id == instanceDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("EC2 instance not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.TerminateInstance(account, record.InstanceId);

            _db.Ec2InstanceRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ---- Sync ----

        public async Task<SyncEc2InstancesResult> SyncInstances(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = Ec2ProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudInstances = await provider.FetchAllInstances(account);

            var dbRecords = await _db.Ec2InstanceRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudInstances)
            {
                var existing = dbRecords.FirstOrDefault(r => r.InstanceId == cloud.InstanceId);
                if (existing is null)
                {
                    _db.Ec2InstanceRecords.Add(MapFromCloud(cloud, account.Provider!, accountId, user));
                    added++;
                }
                else if (HasChanges(existing, cloud))
                {
                    UpdateRecordFromCloud(existing, cloud);
                    updated++;
                }
            }

            var cloudIds = cloudInstances.Select(i => i.InstanceId).ToHashSet();
            var toRemove = dbRecords.Where(r => !cloudIds.Contains(r.InstanceId)).ToList();
            _db.Ec2InstanceRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.Ec2InstanceRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            return new SyncEc2InstancesResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Instances = finalRecords.Select(Map).ToList()
            };
        }
    }
}
