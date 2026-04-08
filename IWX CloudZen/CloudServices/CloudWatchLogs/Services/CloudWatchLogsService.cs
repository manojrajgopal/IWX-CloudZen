using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Entities;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Factory;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.CloudWatchLogs.Services
{
    public class CloudWatchLogsService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public CloudWatchLogsService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ---- Mappers ----

        private static LogGroupResponse MapLogGroup(LogGroupRecord r) => new()
        {
            Id = r.Id,
            LogGroupName = r.LogGroupName,
            Arn = r.Arn,
            RetentionInDays = r.RetentionInDays,
            StoredBytes = r.StoredBytes,
            MetricFilterCount = r.MetricFilterCount,
            KmsKeyId = r.KmsKeyId,
            DataProtectionStatus = r.DataProtectionStatus,
            LogGroupClass = r.LogGroupClass,
            CreationTimeUtc = r.CreationTimeUtc,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static LogStreamResponse MapLogStream(LogStreamRecord r) => new()
        {
            Id = r.Id,
            LogStreamName = r.LogStreamName,
            Arn = r.Arn,
            LogGroupName = r.LogGroupName,
            LogGroupRecordId = r.LogGroupRecordId,
            FirstEventTimestamp = r.FirstEventTimestamp,
            LastEventTimestamp = r.LastEventTimestamp,
            LastIngestionTime = r.LastIngestionTime,
            StoredBytes = r.StoredBytes,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        // ---- Log Group CRUD ----

        public async Task<LogGroupListResponse> ListLogGroups(string user, int accountId)
        {
            var records = await _db.LogGroupRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderBy(x => x.LogGroupName)
                .ToListAsync();

            return new LogGroupListResponse { LogGroups = records.Select(MapLogGroup).ToList() };
        }

        public async Task<LogGroupResponse> GetLogGroup(string user, int accountId, int logGroupDbId)
        {
            var record = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            return MapLogGroup(record);
        }

        public async Task<LogGroupResponse> CreateLogGroup(string user, int accountId, CreateLogGroupRequest request)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.CreateLogGroup(
                account,
                request.LogGroupName,
                request.RetentionInDays,
                request.KmsKeyId,
                request.LogGroupClass);

            var record = new LogGroupRecord
            {
                LogGroupName = info.LogGroupName,
                Arn = info.Arn,
                RetentionInDays = info.RetentionInDays,
                StoredBytes = info.StoredBytes,
                MetricFilterCount = info.MetricFilterCount.ToString(),
                KmsKeyId = info.KmsKeyId,
                DataProtectionStatus = info.DataProtectionStatus,
                LogGroupClass = info.LogGroupClass,
                CreationTimeUtc = info.CreationTimeUtc,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.LogGroupRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapLogGroup(record);
        }

        public async Task<LogGroupResponse> UpdateLogGroup(string user, int accountId, int logGroupDbId, UpdateLogGroupRequest request)
        {
            var record = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.UpdateLogGroup(
                account,
                record.LogGroupName,
                request.RetentionInDays,
                request.KmsKeyId);

            record.RetentionInDays = info.RetentionInDays;
            record.StoredBytes = info.StoredBytes;
            record.MetricFilterCount = info.MetricFilterCount.ToString();
            record.KmsKeyId = info.KmsKeyId;
            record.DataProtectionStatus = info.DataProtectionStatus;
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return MapLogGroup(record);
        }

        public async Task DeleteLogGroup(string user, int accountId, int logGroupDbId)
        {
            var record = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteLogGroup(account, record.LogGroupName);

            // Remove associated log streams from DB
            var streams = await _db.LogStreamRecords
                .Where(x => x.LogGroupRecordId == record.Id)
                .ToListAsync();
            _db.LogStreamRecords.RemoveRange(streams);

            _db.LogGroupRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ---- Log Group Sync ----

        public async Task<SyncLogGroupResult> SyncLogGroups(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudLogGroups = await provider.FetchAllLogGroups(account);

            var dbLogGroups = await _db.LogGroupRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudLogGroups)
            {
                var existing = dbLogGroups.FirstOrDefault(r => r.LogGroupName == cloud.LogGroupName);
                if (existing is null)
                {
                    _db.LogGroupRecords.Add(new LogGroupRecord
                    {
                        LogGroupName = cloud.LogGroupName,
                        Arn = cloud.Arn,
                        RetentionInDays = cloud.RetentionInDays,
                        StoredBytes = cloud.StoredBytes,
                        MetricFilterCount = cloud.MetricFilterCount.ToString(),
                        KmsKeyId = cloud.KmsKeyId,
                        DataProtectionStatus = cloud.DataProtectionStatus,
                        LogGroupClass = cloud.LogGroupClass,
                        CreationTimeUtc = cloud.CreationTimeUtc,
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
                        existing.Arn != cloud.Arn ||
                        existing.RetentionInDays != cloud.RetentionInDays ||
                        existing.StoredBytes != cloud.StoredBytes ||
                        existing.MetricFilterCount != cloud.MetricFilterCount.ToString() ||
                        existing.KmsKeyId != cloud.KmsKeyId ||
                        existing.DataProtectionStatus != cloud.DataProtectionStatus ||
                        existing.LogGroupClass != cloud.LogGroupClass;

                    if (changed)
                    {
                        existing.Arn = cloud.Arn;
                        existing.RetentionInDays = cloud.RetentionInDays;
                        existing.StoredBytes = cloud.StoredBytes;
                        existing.MetricFilterCount = cloud.MetricFilterCount.ToString();
                        existing.KmsKeyId = cloud.KmsKeyId;
                        existing.DataProtectionStatus = cloud.DataProtectionStatus;
                        existing.LogGroupClass = cloud.LogGroupClass;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudNames = cloudLogGroups.Select(g => g.LogGroupName).ToHashSet();
            var toRemove = dbLogGroups.Where(r => !cloudNames.Contains(r.LogGroupName)).ToList();

            // Also remove orphaned log stream records
            var removeIds = toRemove.Select(r => r.Id).ToHashSet();
            var orphanedStreams = await _db.LogStreamRecords
                .Where(s => removeIds.Contains(s.LogGroupRecordId))
                .ToListAsync();
            _db.LogStreamRecords.RemoveRange(orphanedStreams);

            _db.LogGroupRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.LogGroupRecords
                .Where(x => x.CloudAccountId == accountId)
                .OrderBy(x => x.LogGroupName)
                .ToListAsync();

            return new SyncLogGroupResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                LogGroups = finalRecords.Select(MapLogGroup).ToList()
            };
        }

        // ---- Log Stream CRUD ----

        public async Task<LogStreamListResponse> ListLogStreams(string user, int accountId, int logGroupDbId)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var records = await _db.LogStreamRecords
                .Where(x => x.LogGroupRecordId == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderByDescending(x => x.LastEventTimestamp)
                .ToListAsync();

            return new LogStreamListResponse { LogStreams = records.Select(MapLogStream).ToList() };
        }

        public async Task<LogStreamResponse> CreateLogStream(string user, int accountId, int logGroupDbId, CreateLogStreamRequest request)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var info = await provider.CreateLogStream(account, logGroup.LogGroupName, request.LogStreamName);

            var record = new LogStreamRecord
            {
                LogStreamName = info.LogStreamName,
                Arn = info.Arn,
                LogGroupName = logGroup.LogGroupName,
                LogGroupRecordId = logGroup.Id,
                FirstEventTimestamp = info.FirstEventTimestamp,
                LastEventTimestamp = info.LastEventTimestamp,
                LastIngestionTime = info.LastIngestionTime,
                StoredBytes = info.StoredBytes,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.LogStreamRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapLogStream(record);
        }

        public async Task DeleteLogStream(string user, int accountId, int logGroupDbId, int logStreamDbId)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var record = await _db.LogStreamRecords
                .FirstOrDefaultAsync(x => x.Id == logStreamDbId && x.LogGroupRecordId == logGroupDbId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log stream not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.DeleteLogStream(account, logGroup.LogGroupName, record.LogStreamName);

            _db.LogStreamRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ---- Log Stream Sync ----

        public async Task<SyncLogStreamResult> SyncLogStreams(string user, int accountId, int logGroupDbId)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var cloudStreams = await provider.FetchLogStreams(account, logGroup.LogGroupName);

            var dbStreams = await _db.LogStreamRecords
                .Where(x => x.LogGroupRecordId == logGroup.Id && x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudStreams)
            {
                var existing = dbStreams.FirstOrDefault(r => r.LogStreamName == cloud.LogStreamName);
                if (existing is null)
                {
                    _db.LogStreamRecords.Add(new LogStreamRecord
                    {
                        LogStreamName = cloud.LogStreamName,
                        Arn = cloud.Arn,
                        LogGroupName = logGroup.LogGroupName,
                        LogGroupRecordId = logGroup.Id,
                        FirstEventTimestamp = cloud.FirstEventTimestamp,
                        LastEventTimestamp = cloud.LastEventTimestamp,
                        LastIngestionTime = cloud.LastIngestionTime,
                        StoredBytes = cloud.StoredBytes,
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
                        existing.Arn != cloud.Arn ||
                        existing.FirstEventTimestamp != cloud.FirstEventTimestamp ||
                        existing.LastEventTimestamp != cloud.LastEventTimestamp ||
                        existing.LastIngestionTime != cloud.LastIngestionTime ||
                        existing.StoredBytes != cloud.StoredBytes;

                    if (changed)
                    {
                        existing.Arn = cloud.Arn;
                        existing.FirstEventTimestamp = cloud.FirstEventTimestamp;
                        existing.LastEventTimestamp = cloud.LastEventTimestamp;
                        existing.LastIngestionTime = cloud.LastIngestionTime;
                        existing.StoredBytes = cloud.StoredBytes;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudNames = cloudStreams.Select(s => s.LogStreamName).ToHashSet();
            var toRemove = dbStreams.Where(r => !cloudNames.Contains(r.LogStreamName)).ToList();
            _db.LogStreamRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.LogStreamRecords
                .Where(x => x.LogGroupRecordId == logGroup.Id && x.CloudAccountId == accountId)
                .OrderByDescending(x => x.LastEventTimestamp)
                .ToListAsync();

            return new SyncLogStreamResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                LogStreams = finalRecords.Select(MapLogStream).ToList()
            };
        }

        // ---- Log Events (Read/Write - no DB storage) ----

        public async Task<LogEventsListResponse> GetLogEvents(string user, int accountId, int logGroupDbId, string logStreamName, int limit = 100, string? nextToken = null)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return await provider.GetLogEvents(account, logGroup.LogGroupName, logStreamName, limit, nextToken);
        }

        public async Task PutLogEvents(string user, int accountId, int logGroupDbId, DTOs.PutLogEventsRequest request)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            await provider.PutLogEvents(account, logGroup.LogGroupName, request.LogStreamName, request.LogEvents);
        }

        public async Task<LogEventsListResponse> FilterLogEvents(string user, int accountId, int logGroupDbId, FilterLogEventsRequest filter)
        {
            var logGroup = await _db.LogGroupRecords
                .FirstOrDefaultAsync(x => x.Id == logGroupDbId && x.CloudAccountId == accountId && x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Log group not found.");

            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = CloudWatchLogsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return await provider.FilterLogEvents(account, logGroup.LogGroupName, filter);
        }
    }
}
