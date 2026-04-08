using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.Cluster.Entities;
using IWX_CloudZen.CloudServices.ECS.DTOs;
using IWX_CloudZen.CloudServices.ECS.Entities;
using IWX_CloudZen.CloudServices.ECS.Factory;
using IWX_CloudZen.CloudServices.ECS.Interfaces;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace IWX_CloudZen.CloudServices.ECS.Services
{
    public class EcsService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public EcsService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ================================================================
        // Normalizer
        // ================================================================

        /// <summary>
        /// Normalizes a task definition family name to satisfy the AWS ECS constraint:
        /// 1–255 characters; letters (A-Z, a-z), numbers (0-9), hyphens (-), and underscores (_).
        /// Spaces become hyphens; any other invalid character is replaced with a hyphen;
        /// consecutive hyphens/underscores are collapsed; leading/trailing hyphens are trimmed.
        /// </summary>
        public static string NormalizeFamilyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Task definition family name cannot be empty.");

            // Replace whitespace with hyphen
            var normalized = Regex.Replace(name.Trim(), @"\s+", "-");

            // Replace any character that is not a letter, digit, hyphen, or underscore
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9\-_]", "-");

            // Collapse consecutive hyphens/underscores into a single hyphen
            normalized = Regex.Replace(normalized, @"[\-_]{2,}", "-");

            // Trim leading and trailing hyphens or underscores
            normalized = normalized.Trim('-', '_');

            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException($"'{name}' could not be normalized to a valid ECS family name.");

            // AWS limit: 255 characters
            if (normalized.Length > 255)
                normalized = normalized[..255].TrimEnd('-', '_');

            return normalized;
        }

        /// <summary>
        /// Normalizes a container name to satisfy the AWS ECS constraint:
        /// 1–255 characters; letters (A-Z, a-z), numbers (0-9), hyphens (-), and underscores (_).
        /// Spaces become hyphens; any other invalid character is replaced with a hyphen;
        /// consecutive hyphens/underscores are collapsed; leading/trailing hyphens are trimmed.
        /// </summary>
        public static string NormalizeContainerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Container name cannot be empty.");

            // Replace whitespace with hyphen
            var normalized = Regex.Replace(name.Trim(), @"\s+", "-");

            // Replace any character that is not a letter, digit, hyphen, or underscore
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9\-_]", "-");

            // Collapse consecutive hyphens/underscores into a single hyphen
            normalized = Regex.Replace(normalized, @"[\-_]{2,}", "-");

            // Trim leading and trailing hyphens or underscores
            normalized = normalized.Trim('-', '_');

            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException($"'{name}' could not be normalized to a valid ECS container name.");

            // AWS limit: 255 characters
            if (normalized.Length > 255)
                normalized = normalized[..255].TrimEnd('-', '_');

            return normalized;
        }

        // ================================================================
        // Resolve credentials + provider
        // ================================================================

        private async Task<(CloudAccounts.DTOs.CloudConnectionSecrets account, IEcsProvider provider)>
            Resolve(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = EcsProviderFactory.Get(
                account.Provider ?? throw new InvalidOperationException("Cloud provider is not set."));

            return (account, provider);
        }

        // ================================================================
        // Mappers
        // ================================================================

        private static TaskDefinitionResponse MapTaskDef(EcsTaskDefinitionRecord r) => new()
        {
            Id = r.Id,
            Family = r.Family,
            TaskDefinitionArn = r.TaskDefinitionArn,
            Revision = r.Revision,
            Status = r.Status,
            Cpu = r.Cpu,
            Memory = r.Memory,
            NetworkMode = r.NetworkMode,
            ExecutionRoleArn = r.ExecutionRoleArn,
            TaskRoleArn = r.TaskRoleArn,
            RequiresCompatibilities = r.RequiresCompatibilities,
            OsFamily = r.OsFamily,
            ContainerCount = r.ContainerCount,
            ContainerDefinitionsJson = r.ContainerDefinitionsJson,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static EcsServiceResponse MapService(EcsServiceRecord r) => new()
        {
            Id = r.Id,
            ServiceName = r.ServiceName,
            ServiceArn = r.ServiceArn,
            ClusterName = r.ClusterName,
            ClusterArn = r.ClusterArn,
            TaskDefinition = r.TaskDefinition,
            DesiredCount = r.DesiredCount,
            RunningCount = r.RunningCount,
            PendingCount = r.PendingCount,
            Status = r.Status,
            LaunchType = r.LaunchType,
            SchedulingStrategy = r.SchedulingStrategy,
            NetworkConfigurationJson = r.NetworkConfigurationJson,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            ServiceCreatedAt = r.ServiceCreatedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static EcsTaskResponse MapTask(EcsTaskRecord r) => new()
        {
            Id = r.Id,
            TaskArn = r.TaskArn,
            ClusterName = r.ClusterName,
            ClusterArn = r.ClusterArn,
            TaskDefinitionArn = r.TaskDefinitionArn,
            Group = r.Group,
            LastStatus = r.LastStatus,
            DesiredStatus = r.DesiredStatus,
            Cpu = r.Cpu,
            Memory = r.Memory,
            LaunchType = r.LaunchType,
            Connectivity = r.Connectivity,
            StopCode = r.StopCode,
            StoppedReason = r.StoppedReason,
            StartedAt = r.StartedAt,
            StoppedAt = r.StoppedAt,
            PullStartedAt = r.PullStartedAt,
            PullStoppedAt = r.PullStoppedAt,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        // ================================================================
        // Task Definitions — CRUD
        // ================================================================

        public async Task<TaskDefinitionListResponse> ListTaskDefinitions(
            string user, int accountId, string? family = null)
        {
            var query = _db.EcsTaskDefinitionRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(family))
                query = query.Where(x => x.Family == family);

            var records = await query
                .OrderBy(x => x.Family)
                .ThenByDescending(x => x.Revision)
                .ToListAsync();

            return new TaskDefinitionListResponse
            {
                TotalCount = records.Count,
                FamilyFilter = family,
                TaskDefinitions = records.Select(MapTaskDef).ToList()
            };
        }

        public async Task<TaskDefinitionResponse> GetTaskDefinition(
            string user, int accountId, int id)
        {
            var record = await _db.EcsTaskDefinitionRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Task definition not found.");

            return MapTaskDef(record);
        }

        public async Task<TaskDefinitionResponse> RegisterTaskDefinition(
            string user, int accountId, RegisterTaskDefinitionRequest request)
        {
            var (account, provider) = await Resolve(user, accountId);

            // Normalize to satisfy AWS ECS family name constraints
            request.Family = NormalizeFamilyName(request.Family);

            // Normalize all container names to satisfy AWS ECS constraints
            foreach (var container in request.ContainerDefinitions)
                container.Name = NormalizeContainerName(container.Name);

            var cloudInfo = await provider.RegisterTaskDefinition(account, request);

            var record = new EcsTaskDefinitionRecord
            {
                Family = cloudInfo.Family,
                TaskDefinitionArn = cloudInfo.TaskDefinitionArn,
                Revision = cloudInfo.Revision,
                Status = cloudInfo.Status,
                Cpu = cloudInfo.Cpu,
                Memory = cloudInfo.Memory,
                NetworkMode = cloudInfo.NetworkMode,
                ExecutionRoleArn = cloudInfo.ExecutionRoleArn,
                TaskRoleArn = cloudInfo.TaskRoleArn,
                RequiresCompatibilities = cloudInfo.RequiresCompatibilities,
                OsFamily = cloudInfo.OsFamily,
                ContainerDefinitionsJson = cloudInfo.ContainerDefinitionsJson,
                ContainerCount = cloudInfo.ContainerCount,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.EcsTaskDefinitionRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapTaskDef(record);
        }

        /// <summary>
        /// Marks a task definition revision as INACTIVE in the cloud and updates the DB record.
        /// </summary>
        public async Task<TaskDefinitionResponse> DeregisterTaskDefinition(
            string user, int accountId, int id)
        {
            var record = await _db.EcsTaskDefinitionRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Task definition not found.");

            if (string.IsNullOrEmpty(record.TaskDefinitionArn))
                throw new InvalidOperationException("Task definition ARN is missing; cannot deregister.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.DeregisterTaskDefinition(account, record.TaskDefinitionArn);

            record.Status = cloudInfo.Status; // INACTIVE
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapTaskDef(record);
        }

        /// <summary>
        /// Permanently deletes a task definition from the cloud (must be INACTIVE first) and removes from DB.
        /// </summary>
        public async Task DeleteTaskDefinition(string user, int accountId, int id)
        {
            var record = await _db.EcsTaskDefinitionRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Task definition not found.");

            if (string.IsNullOrEmpty(record.TaskDefinitionArn))
                throw new InvalidOperationException("Task definition ARN is missing; cannot delete.");

            var (account, provider) = await Resolve(user, accountId);

            // Deregister first if still ACTIVE
            if (record.Status == "ACTIVE")
                await provider.DeregisterTaskDefinition(account, record.TaskDefinitionArn);

            await provider.DeleteTaskDefinition(account, record.TaskDefinitionArn);

            _db.EcsTaskDefinitionRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // Services — CRUD
        // ================================================================

        public async Task<EcsServiceListResponse> ListServices(
            string user, int accountId, string? clusterName = null)
        {
            var query = _db.EcsServiceRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(clusterName))
                query = query.Where(x => x.ClusterName == clusterName);

            var records = await query
                .OrderBy(x => x.ClusterName)
                .ThenBy(x => x.ServiceName)
                .ToListAsync();

            return new EcsServiceListResponse
            {
                ClusterName = clusterName ?? "all",
                TotalCount = records.Count,
                Services = records.Select(MapService).ToList()
            };
        }

        public async Task<EcsServiceResponse> GetService(string user, int accountId, int id)
        {
            var record = await _db.EcsServiceRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Service not found.");

            return MapService(record);
        }

        public async Task<EcsServiceResponse> CreateService(
            string user, int accountId, CreateEcsServiceRequest request)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.CreateService(account, request);

            var record = new EcsServiceRecord
            {
                ServiceName = cloudInfo.ServiceName,
                ServiceArn = cloudInfo.ServiceArn,
                ClusterName = cloudInfo.ClusterName,
                ClusterArn = cloudInfo.ClusterArn,
                TaskDefinition = cloudInfo.TaskDefinition,
                DesiredCount = cloudInfo.DesiredCount,
                RunningCount = cloudInfo.RunningCount,
                PendingCount = cloudInfo.PendingCount,
                Status = cloudInfo.Status,
                LaunchType = cloudInfo.LaunchType,
                SchedulingStrategy = cloudInfo.SchedulingStrategy,
                NetworkConfigurationJson = cloudInfo.NetworkConfigurationJson,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                ServiceCreatedAt = cloudInfo.ServiceCreatedAt,
                CreatedAt = DateTime.UtcNow
            };

            _db.EcsServiceRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapService(record);
        }

        public async Task<EcsServiceResponse> UpdateService(
            string user, int accountId, int id, UpdateEcsServiceRequest request)
        {
            var record = await _db.EcsServiceRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Service not found.");

            var (account, provider) = await Resolve(user, accountId);

            var cloudInfo = await provider.UpdateService(
                account,
                record.ClusterName,
                record.ServiceName,
                request.DesiredCount,
                request.TaskDefinition);

            record.DesiredCount = cloudInfo.DesiredCount;
            record.RunningCount = cloudInfo.RunningCount;
            record.PendingCount = cloudInfo.PendingCount;
            record.Status = cloudInfo.Status;

            if (!string.IsNullOrWhiteSpace(request.TaskDefinition))
                record.TaskDefinition = cloudInfo.TaskDefinition;

            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapService(record);
        }

        public async Task DeleteService(string user, int accountId, int id)
        {
            var record = await _db.EcsServiceRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Service not found.");

            var (account, provider) = await Resolve(user, accountId);

            await provider.DeleteService(account, record.ClusterName, record.ServiceName);

            _db.EcsServiceRecords.Remove(record);
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // Tasks — CRUD
        // ================================================================

        public async Task<EcsTaskListResponse> ListTasks(
            string user, int accountId, string? clusterName = null)
        {
            var query = _db.EcsTaskRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user);

            if (!string.IsNullOrWhiteSpace(clusterName))
                query = query.Where(x => x.ClusterName == clusterName);

            var records = await query
                .OrderBy(x => x.ClusterName)
                .ThenByDescending(x => x.StartedAt)
                .ToListAsync();

            return new EcsTaskListResponse
            {
                ClusterName = clusterName ?? "all",
                TotalCount = records.Count,
                Tasks = records.Select(MapTask).ToList()
            };
        }

        public async Task<EcsTaskResponse> GetTask(string user, int accountId, int id)
        {
            var record = await _db.EcsTaskRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Task not found.");

            return MapTask(record);
        }

        public async Task<List<EcsTaskResponse>> RunTask(
            string user, int accountId, RunTaskRequest request)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudTasks = await provider.RunTask(account, request);

            var records = cloudTasks.Select(t => new EcsTaskRecord
            {
                TaskArn = t.TaskArn,
                ClusterName = t.ClusterName,
                ClusterArn = t.ClusterArn,
                TaskDefinitionArn = t.TaskDefinitionArn,
                Group = t.Group,
                LastStatus = t.LastStatus,
                DesiredStatus = t.DesiredStatus,
                Cpu = t.Cpu,
                Memory = t.Memory,
                LaunchType = t.LaunchType,
                Connectivity = t.Connectivity,
                StopCode = t.StopCode,
                StoppedReason = t.StoppedReason,
                StartedAt = t.StartedAt,
                StoppedAt = t.StoppedAt,
                PullStartedAt = t.PullStartedAt,
                PullStoppedAt = t.PullStoppedAt,
                Provider = account.Provider!,
                CloudAccountId = accountId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.EcsTaskRecords.AddRange(records);
            await _db.SaveChangesAsync();

            return records.Select(MapTask).ToList();
        }

        public async Task StopTask(string user, int accountId, int id, string? reason)
        {
            var record = await _db.EcsTaskRecords
                .FirstOrDefaultAsync(x => x.Id == id &&
                                          x.CloudAccountId == accountId &&
                                          x.CreatedBy == user)
                ?? throw new KeyNotFoundException("Task not found.");

            var (account, provider) = await Resolve(user, accountId);

            await provider.StopTask(account, record.ClusterName, record.TaskArn, reason);

            // Optimistically mark as stopped in DB until next sync
            record.DesiredStatus = "STOPPED";
            record.StoppedReason = reason ?? "Stopped by IWX CloudZen";
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ================================================================
        // Task Definitions — Sync
        // ================================================================

        public async Task<SyncTaskDefinitionsResult> SyncTaskDefinitions(
            string user, int accountId)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudDefs = await provider.FetchAllTaskDefinitions(account);

            var dbDefs = await _db.EcsTaskDefinitionRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudDefs)
            {
                // Each task definition revision is uniquely identified by its ARN
                var existing = dbDefs.FirstOrDefault(d => d.TaskDefinitionArn == cloud.TaskDefinitionArn);

                if (existing is null)
                {
                    _db.EcsTaskDefinitionRecords.Add(new EcsTaskDefinitionRecord
                    {
                        Family = cloud.Family,
                        TaskDefinitionArn = cloud.TaskDefinitionArn,
                        Revision = cloud.Revision,
                        Status = cloud.Status,
                        Cpu = cloud.Cpu,
                        Memory = cloud.Memory,
                        NetworkMode = cloud.NetworkMode,
                        ExecutionRoleArn = cloud.ExecutionRoleArn,
                        TaskRoleArn = cloud.TaskRoleArn,
                        RequiresCompatibilities = cloud.RequiresCompatibilities,
                        OsFamily = cloud.OsFamily,
                        ContainerDefinitionsJson = cloud.ContainerDefinitionsJson,
                        ContainerCount = cloud.ContainerCount,
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
                        existing.Status != cloud.Status ||
                        existing.ContainerCount != cloud.ContainerCount ||
                        existing.ContainerDefinitionsJson != cloud.ContainerDefinitionsJson;

                    if (changed)
                    {
                        existing.Status = cloud.Status;
                        existing.ContainerCount = cloud.ContainerCount;
                        existing.ContainerDefinitionsJson = cloud.ContainerDefinitionsJson;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // Remove DB records whose ARNs no longer exist in the cloud
            var cloudArns = cloudDefs
                .Select(d => d.TaskDefinitionArn)
                .Where(a => a != null)
                .ToHashSet();

            var toRemove = dbDefs
                .Where(d => !string.IsNullOrEmpty(d.TaskDefinitionArn) &&
                            !cloudArns.Contains(d.TaskDefinitionArn))
                .ToList();

            _db.EcsTaskDefinitionRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.EcsTaskDefinitionRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderBy(x => x.Family)
                .ThenByDescending(x => x.Revision)
                .ToListAsync();

            return new SyncTaskDefinitionsResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                TaskDefinitions = finalRecords.Select(MapTaskDef).ToList()
            };
        }

        // ================================================================
        // Services — Sync
        // ================================================================

        public async Task<SyncEcsServicesResult> SyncServices(
            string user, int accountId, string clusterName)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudServices = await provider.FetchAllServices(account, clusterName);

            var dbServices = await _db.EcsServiceRecords
                .Where(x => x.CloudAccountId == accountId &&
                            x.CreatedBy == user &&
                            x.ClusterName == clusterName)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudServices)
            {
                var existing = dbServices.FirstOrDefault(s =>
                    s.ServiceName == cloud.ServiceName && s.ClusterName == cloud.ClusterName);

                if (existing is null)
                {
                    _db.EcsServiceRecords.Add(new EcsServiceRecord
                    {
                        ServiceName = cloud.ServiceName,
                        ServiceArn = cloud.ServiceArn,
                        ClusterName = cloud.ClusterName,
                        ClusterArn = cloud.ClusterArn,
                        TaskDefinition = cloud.TaskDefinition,
                        DesiredCount = cloud.DesiredCount,
                        RunningCount = cloud.RunningCount,
                        PendingCount = cloud.PendingCount,
                        Status = cloud.Status,
                        LaunchType = cloud.LaunchType,
                        SchedulingStrategy = cloud.SchedulingStrategy,
                        NetworkConfigurationJson = cloud.NetworkConfigurationJson,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        ServiceCreatedAt = cloud.ServiceCreatedAt,
                        CreatedAt = DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    bool changed =
                        existing.Status != cloud.Status ||
                        existing.DesiredCount != cloud.DesiredCount ||
                        existing.RunningCount != cloud.RunningCount ||
                        existing.PendingCount != cloud.PendingCount ||
                        existing.TaskDefinition != cloud.TaskDefinition;

                    if (changed)
                    {
                        existing.Status = cloud.Status;
                        existing.DesiredCount = cloud.DesiredCount;
                        existing.RunningCount = cloud.RunningCount;
                        existing.PendingCount = cloud.PendingCount;
                        existing.TaskDefinition = cloud.TaskDefinition;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            var cloudNames = cloudServices.Select(s => s.ServiceName).ToHashSet();
            var toRemove = dbServices
                .Where(s => !cloudNames.Contains(s.ServiceName))
                .ToList();

            _db.EcsServiceRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.EcsServiceRecords
                .Where(x => x.CloudAccountId == accountId &&
                            x.CreatedBy == user &&
                            x.ClusterName == clusterName)
                .OrderBy(x => x.ServiceName)
                .ToListAsync();

            return new SyncEcsServicesResult
            {
                ClusterName = clusterName,
                Added = added,
                Updated = updated,
                Removed = removed,
                Services = finalRecords.Select(MapService).ToList()
            };
        }

        // ================================================================
        // Tasks — Sync
        // ================================================================

        public async Task<SyncEcsTasksResult> SyncTasks(
            string user, int accountId, string clusterName)
        {
            var (account, provider) = await Resolve(user, accountId);

            var cloudTasks = await provider.FetchAllTasks(account, clusterName);

            var dbTasks = await _db.EcsTaskRecords
                .Where(x => x.CloudAccountId == accountId &&
                            x.CreatedBy == user &&
                            x.ClusterName == clusterName)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudTasks)
            {
                var existing = dbTasks.FirstOrDefault(t => t.TaskArn == cloud.TaskArn);

                if (existing is null)
                {
                    _db.EcsTaskRecords.Add(new EcsTaskRecord
                    {
                        TaskArn = cloud.TaskArn,
                        ClusterName = cloud.ClusterName,
                        ClusterArn = cloud.ClusterArn,
                        TaskDefinitionArn = cloud.TaskDefinitionArn,
                        Group = cloud.Group,
                        LastStatus = cloud.LastStatus,
                        DesiredStatus = cloud.DesiredStatus,
                        Cpu = cloud.Cpu,
                        Memory = cloud.Memory,
                        LaunchType = cloud.LaunchType,
                        Connectivity = cloud.Connectivity,
                        StopCode = cloud.StopCode,
                        StoppedReason = cloud.StoppedReason,
                        StartedAt = cloud.StartedAt,
                        StoppedAt = cloud.StoppedAt,
                        PullStartedAt = cloud.PullStartedAt,
                        PullStoppedAt = cloud.PullStoppedAt,
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
                        existing.LastStatus != cloud.LastStatus ||
                        existing.DesiredStatus != cloud.DesiredStatus ||
                        existing.Connectivity != cloud.Connectivity ||
                        existing.StopCode != cloud.StopCode ||
                        existing.StoppedAt != cloud.StoppedAt;

                    if (changed)
                    {
                        existing.LastStatus = cloud.LastStatus;
                        existing.DesiredStatus = cloud.DesiredStatus;
                        existing.Connectivity = cloud.Connectivity;
                        existing.StopCode = cloud.StopCode;
                        existing.StoppedReason = cloud.StoppedReason;
                        existing.StoppedAt = cloud.StoppedAt;
                        existing.StartedAt = cloud.StartedAt ?? existing.StartedAt;
                        existing.PullStartedAt = cloud.PullStartedAt ?? existing.PullStartedAt;
                        existing.PullStoppedAt = cloud.PullStoppedAt ?? existing.PullStoppedAt;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // Remove tasks that no longer appear in the cloud
            var cloudArns = cloudTasks.Select(t => t.TaskArn).ToHashSet();
            var toRemove = dbTasks.Where(t => !cloudArns.Contains(t.TaskArn)).ToList();
            _db.EcsTaskRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.EcsTaskRecords
                .Where(x => x.CloudAccountId == accountId &&
                            x.CreatedBy == user &&
                            x.ClusterName == clusterName)
                .OrderByDescending(x => x.StartedAt)
                .ToListAsync();

            return new SyncEcsTasksResult
            {
                ClusterName = clusterName,
                Added = added,
                Updated = updated,
                Removed = removed,
                Tasks = finalRecords.Select(MapTask).ToList()
            };
        }

        // ================================================================
        // Full Sync (task definitions + all known clusters)
        // ================================================================

        /// <summary>
        /// Syncs task definitions across the account, then syncs services and tasks
        /// for every cluster recorded in the database for this account.
        /// </summary>
        public async Task<FullEcsSyncResult> SyncAll(string user, int accountId)
        {
            // 1 — Sync task definitions (account-wide, no cluster needed)
            var taskDefResult = await SyncTaskDefinitions(user, accountId);

            // 2 — Get all known clusters for this account from DB (managed by ClusterService)
            var clusterNames = await _db.ClusterRecords
                .Where(c => c.CloudAccountId == accountId && c.CreatedBy == user)
                .Select(c => c.Name)
                .ToListAsync();

            var serviceResults = new List<SyncEcsServicesResult>();
            var taskResults = new List<SyncEcsTasksResult>();

            foreach (var clusterName in clusterNames)
            {
                var svcResult = await SyncServices(user, accountId, clusterName);
                serviceResults.Add(svcResult);

                var taskResult = await SyncTasks(user, accountId, clusterName);
                taskResults.Add(taskResult);
            }

            return new FullEcsSyncResult
            {
                TaskDefinitions = taskDefResult,
                ServicesByCluster = serviceResults,
                TasksByCluster = taskResults,
                ClustersSynced = clusterNames,
                SyncedAt = DateTime.UtcNow
            };
        }
    }
}
