using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.Data;
using IWX_CloudZen.Permissions.DTOs;
using IWX_CloudZen.Permissions.Entities;
using IWX_CloudZen.Permissions.Factory;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.Permissions.Services
{
    public class PermissionsService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public PermissionsService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        private static PolicyRecordResponse Map(PolicyRecord r) => new()
        {
            Id = r.Id,
            PolicyArn = r.PolicyArn,
            PolicyName = r.PolicyName,
            PolicyType = r.PolicyType,
            AttachedVia = r.AttachedVia,
            Provider = r.Provider,
            CloudAccountId = r.CloudAccountId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private async Task<(CloudAccounts.DTOs.CloudConnectionSecrets account,
            Interfaces.IPermissionsProvider provider)> Resolve(string user, int accountId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = PermissionsProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return (account, provider);
        }

        /// <summary>
        /// Lists policies stored in the database for this account (fast, no AWS call).
        /// </summary>
        public async Task<PolicyRecordListResponse> ListPolicies(string user, int accountId)
        {
            var records = await _db.PolicyRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderBy(x => x.PolicyName)
                .ToListAsync();

            return new PolicyRecordListResponse
            {
                TotalPolicies = records.Count,
                Policies = records.Select(Map).ToList()
            };
        }

        /// <summary>
        /// Lists every policy stored in the database — no AWS call.
        /// </summary>
        public async Task<PolicyListResponse> GetAllPolicies(string user, int accountId)
        {
            var records = await _db.PolicyRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .OrderBy(x => x.PolicyName)
                .ToListAsync();

            var policies = records.Select(r => new PolicyResponse
            {
                PolicyArn = r.PolicyArn,
                PolicyName = r.PolicyName,
                PolicyType = r.PolicyType,
                AttachedVia = r.AttachedVia,
                Statements = new()
            }).ToList();

            return new PolicyListResponse
            {
                UserArn = string.Empty,
                TotalPolicies = policies.Count,
                Policies = policies
            };
        }

        /// <summary>
        /// Returns a lightweight summary derived from the database — no AWS call.
        /// </summary>
        public async Task<PermissionSummaryResponse> GetSummary(string user, int accountId)
        {
            var records = await _db.PolicyRecords
                .Where(x => x.CloudAccountId == accountId && x.CreatedBy == user)
                .ToListAsync();

            var attachedManaged = records.Count(r =>
                r.AttachedVia == "User" &&
                (r.PolicyType == "AWS Managed" || r.PolicyType == "Customer Managed"));

            var inlineCount = records.Count(r =>
                r.AttachedVia == "User" && r.PolicyType == "Inline");

            var groupPolicies = records.Where(r => r.AttachedVia.StartsWith("Group:")).ToList();

            var groups = groupPolicies
                .Select(r => r.AttachedVia.Replace("Group: ", string.Empty).Trim())
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            var policies = records.Select(r => new PolicyAttachmentInfo
            {
                PolicyArn = r.PolicyArn,
                PolicyName = r.PolicyName,
                Type = r.PolicyType,
                AttachedVia = r.AttachedVia
            }).ToList();

            return new PermissionSummaryResponse
            {
                UserName = user,
                UserArn = string.Empty,
                AttachedManagedPoliciesCount = attachedManaged,
                InlinePoliciesCount = inlineCount,
                GroupPoliciesCount = groupPolicies.Count,
                Groups = groups,
                Policies = policies
            };
        }

        /// <summary>
        /// Simulates specified IAM actions and returns an allowed/denied decision per action.
        /// </summary>
        public async Task<PermissionCheckResponse> CheckPermissions(
            string user, int accountId, CheckPermissionRequest request)
        {
            var (account, provider) = await Resolve(user, accountId);
            return await provider.CheckPermissions(account, request.Actions, request.ResourceArns);
        }

        /// <summary>
        /// Attaches a managed policy to the IAM user and persists the record to the database.
        /// </summary>
        public async Task<PolicyRecordResponse> AttachPolicy(string user, int accountId, string policyArn)
        {
            var (account, provider) = await Resolve(user, accountId);
            await provider.AttachPolicy(account, policyArn);

            // Resolve display name from ARN
            var policyName = policyArn.Contains('/')
                ? policyArn.Split('/').Last()
                : policyArn;

            var policyType = policyArn.StartsWith("arn:aws:iam::aws:", StringComparison.OrdinalIgnoreCase)
                ? "AWS Managed"
                : "Customer Managed";

            // Upsert: avoid duplicate if already synced
            var existing = await _db.PolicyRecords.FirstOrDefaultAsync(x =>
                x.PolicyArn == policyArn &&
                x.AttachedVia == "User" &&
                x.CloudAccountId == accountId);

            if (existing is null)
            {
                var record = new PolicyRecord
                {
                    PolicyArn = policyArn,
                    PolicyName = policyName,
                    PolicyType = policyType,
                    AttachedVia = "User",
                    Provider = account.Provider!,
                    CloudAccountId = accountId,
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                };
                _db.PolicyRecords.Add(record);
                await _db.SaveChangesAsync();
                return Map(record);
            }

            return Map(existing);
        }

        /// <summary>
        /// Detaches a managed policy from the IAM user and removes the record from the database.
        /// </summary>
        public async Task DetachPolicy(string user, int accountId, string policyArn)
        {
            var (account, provider) = await Resolve(user, accountId);
            await provider.DetachPolicy(account, policyArn);

            var records = await _db.PolicyRecords
                .Where(x => x.PolicyArn == policyArn &&
                            x.AttachedVia == "User" &&
                            x.CloudAccountId == accountId)
                .ToListAsync();

            if (records.Count > 0)
            {
                _db.PolicyRecords.RemoveRange(records);
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Lists available policies that can be attached.
        /// scope: "AWS" | "Local" | "All" (defaults to "AWS")
        /// </summary>
        public async Task<AvailablePoliciesListResponse> ListAvailablePolicies(
            string user, int accountId, string scope, string? search)
        {
            var (account, provider) = await Resolve(user, accountId);
            return await provider.ListAvailablePolicies(account, scope, search);
        }

        /// <summary>
        /// Syncs all policies from AWS into the database (Added / Updated / Removed).
        /// Uses GetSummary for efficiency — no full statement parsing needed.
        /// </summary>
        public async Task<SyncPoliciesResult> SyncPolicies(string user, int accountId)
        {
            var (account, provider) = await Resolve(user, accountId);

            // Fetch live summary from AWS
            var liveSummary = await provider.GetSummary(account);
            var cloudPolicies = liveSummary.Policies;

            var dbPolicies = await _db.PolicyRecords
                .Where(x => x.CloudAccountId == accountId)
                .ToListAsync();

            int added = 0, updated = 0, removed = 0;

            foreach (var cloud in cloudPolicies)
            {
                // Match on PolicyName + AttachedVia (handles inline + duplicates via group/user)
                var existing = dbPolicies.FirstOrDefault(r =>
                    r.PolicyName == cloud.PolicyName &&
                    r.AttachedVia == cloud.AttachedVia);

                if (existing is null)
                {
                    _db.PolicyRecords.Add(new PolicyRecord
                    {
                        PolicyArn = cloud.PolicyArn,
                        PolicyName = cloud.PolicyName,
                        PolicyType = cloud.Type,
                        AttachedVia = cloud.AttachedVia,
                        Provider = account.Provider!,
                        CloudAccountId = accountId,
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    });
                    added++;
                }
                else
                {
                    bool changed = existing.PolicyArn != cloud.PolicyArn ||
                                   existing.PolicyType != cloud.Type;

                    if (changed)
                    {
                        existing.PolicyArn = cloud.PolicyArn;
                        existing.PolicyType = cloud.Type;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updated++;
                    }
                }
            }

            // Remove DB records no longer in AWS
            var cloudKeys = cloudPolicies
                .Select(p => (p.PolicyName, p.AttachedVia))
                .ToHashSet();

            var toRemove = dbPolicies
                .Where(r => !cloudKeys.Contains((r.PolicyName, r.AttachedVia)))
                .ToList();

            _db.PolicyRecords.RemoveRange(toRemove);
            removed = toRemove.Count;

            await _db.SaveChangesAsync();

            var finalRecords = await _db.PolicyRecords
                .Where(x => x.CloudAccountId == accountId)
                .OrderBy(x => x.PolicyName)
                .ToListAsync();

            return new SyncPoliciesResult
            {
                Added = added,
                Updated = updated,
                Removed = removed,
                Policies = finalRecords.Select(Map).ToList()
            };
        }
    }
}
