using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.Cluster.DTOs;
using IWX_CloudZen.CloudServices.Cluster.Entities;
using IWX_CloudZen.CloudServices.Cluster.Factory;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.Cluster.Services
{
    public class ClusterService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public ClusterService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        private static ClusterResponse Map(ClusterRecord r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            ClusterArn = r.ClusterArn,
            Status = r.Status,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            ContainerInsightsEnabled = r.ContainerInsightsEnabled,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        public async Task<ClusterListResponse> ListClusters(string user, int accountId)
        {
            var records = await _db.ClusterRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            return new ClusterListResponse { Clusters = records.Select(Map).ToList() };
        }

        public async Task<ClusterResponse> CreateCluster(string user, int accountId, string clusterName)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = ClusterProviderFactory.Get(account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            var awsResult = await provider.CreateCluster(account, clusterName);

            var record = new ClusterRecord
            {
                Name = clusterName,
                ClusterArn = awsResult.ClusterArn,
                Status = awsResult.Status,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                ContainerInsightsEnabled = false,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.ClusterRecords.Add(record);
            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task<ClusterResponse> UpdateCluster(string user, int accountId, int clusterId, bool enableContainerInsights)
        {
            var record = await _db.ClusterRecords
                .FirstOrDefaultAsync(x => x.Id == clusterId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Cluster not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = ClusterProviderFactory.Get(account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            var awsResult = await provider.UpdateCluster(account, record.Name, enableContainerInsights);

            record.Status = awsResult.Status;
            record.ContainerInsightsEnabled = enableContainerInsights;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(record);
        }

        public async Task<DeleteClusterResponse> DeleteCluster(string user, int accountId, int clusterId)
        {
            var record = await _db.ClusterRecords
                .FirstOrDefaultAsync(x => x.Id == clusterId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Cluster not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = ClusterProviderFactory.Get(account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            var awsResult = await provider.DeleteCluster(account, record.Name);

            _db.ClusterRecords.Remove(record);
            await _db.SaveChangesAsync();

            return awsResult;
        }

        public async Task<SyncResult> SyncClusters(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = ClusterProviderFactory.Get(account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            // Fetch live clusters from cloud
            var cloudClusters = await provider.FetchAllClusters(account);

            // Load existing DB records for this account
            var dbRecords = await _db.ClusterRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            // --- Added: in cloud but not in DB (match by ARN, fall back to name) ---
            foreach (var cloud in cloudClusters)
            {
                var existing = dbRecords.FirstOrDefault(r =>
                    (!string.IsNullOrEmpty(r.ClusterArn) && r.ClusterArn == cloud.ClusterArn) ||
                    r.Name == cloud.Name);

                if (existing is null)
                {
                    _db.ClusterRecords.Add(new ClusterRecord
                    {
                        Name = cloud.Name,
                        ClusterArn = cloud.ClusterArn,
                        Status = cloud.Status,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        ContainerInsightsEnabled = cloud.ContainerInsightsEnabled,
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    // --- Updated: exists in both, sync status & settings ---
                    bool changed = existing.Status != cloud.Status ||
                                   existing.ClusterArn != cloud.ClusterArn ||
                                   existing.ContainerInsightsEnabled != cloud.ContainerInsightsEnabled;

                    if (changed)
                    {
                        existing.Status = cloud.Status;
                        existing.ClusterArn = cloud.ClusterArn;
                        existing.ContainerInsightsEnabled = cloud.ContainerInsightsEnabled;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // --- Removed: in DB but no longer in cloud ---
            var cloudArns = cloudClusters.Select(c => c.ClusterArn).ToHashSet();
            var cloudNames = cloudClusters.Select(c => c.Name).ToHashSet();

            var toRemove = dbRecords.Where(r =>
                !cloudArns.Contains(r.ClusterArn ?? string.Empty) &&
                !cloudNames.Contains(r.Name)).ToList();

            _db.ClusterRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.ClusterRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            return new SyncResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Clusters = finalRecords.Select(Map).ToList()
            };
        }
    }
}
