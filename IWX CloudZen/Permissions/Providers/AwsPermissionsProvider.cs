using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.Permissions.DTOs;
using IWX_CloudZen.Permissions.Interfaces;
using System.Text.Json;

namespace IWX_CloudZen.Permissions.Providers
{
    public class AwsPermissionsProvider : IPermissionsProvider
    {
        // ---- Client factory ----

        private static AmazonIdentityManagementServiceClient GetClient(CloudConnectionSecrets account)
        {
            return new AmazonIdentityManagementServiceClient(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));
        }

        // ---- Helpers ----

        private static List<PolicyStatementResponse> ParsePolicyDocument(string encodedDocument)
        {
            var json = Uri.UnescapeDataString(encodedDocument);
            var statements = new List<PolicyStatementResponse>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("Statement", out var statementElement))
                return statements;

            // Statement can be a single object or an array
            var statementArray = statementElement.ValueKind == JsonValueKind.Array
                ? statementElement.EnumerateArray().ToList()
                : new List<JsonElement> { statementElement };

            foreach (var stmt in statementArray)
            {
                var entry = new PolicyStatementResponse
                {
                    Sid = stmt.TryGetProperty("Sid", out var sid) ? sid.GetString() ?? "" : "",
                    Effect = stmt.TryGetProperty("Effect", out var effect) ? effect.GetString() ?? "" : ""
                };

                if (stmt.TryGetProperty("Action", out var action))
                    entry.Actions = ReadStringOrArray(action);

                if (stmt.TryGetProperty("NotAction", out var notAction))
                    entry.NotActions = ReadStringOrArray(notAction);

                if (stmt.TryGetProperty("Resource", out var resource))
                    entry.Resources = ReadStringOrArray(resource);

                statements.Add(entry);
            }

            return statements;
        }

        private static List<string> ReadStringOrArray(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return [element.GetString()!];

            if (element.ValueKind == JsonValueKind.Array)
                return element.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => s.Length > 0)
                    .ToList();

            return [];
        }

        private static string ResolvePolicyType(string policyArn)
        {
            if (policyArn.StartsWith("arn:aws:iam::aws:", StringComparison.OrdinalIgnoreCase))
                return "AWS Managed";
            if (policyArn == "Inline")
                return "Inline";
            return "Customer Managed";
        }

        private async Task<List<PolicyStatementResponse>> FetchManagedPolicyStatements(
            AmazonIdentityManagementServiceClient iam, string policyArn)
        {
            try
            {
                var policyResponse = await iam.GetPolicyAsync(new GetPolicyRequest { PolicyArn = policyArn });
                var versionId = policyResponse.Policy.DefaultVersionId;

                var versionResponse = await iam.GetPolicyVersionAsync(new GetPolicyVersionRequest
                {
                    PolicyArn = policyArn,
                    VersionId = versionId
                });

                return ParsePolicyDocument(versionResponse.PolicyVersion.Document);
            }
            catch
            {
                return [];
            }
        }

        private async Task<string> GetUserName(AmazonIdentityManagementServiceClient iam)
        {
            var response = await iam.GetUserAsync(new GetUserRequest());
            return response.User.UserName;
        }

        private async Task<(string UserName, string UserArn)> GetUserInfo(
            AmazonIdentityManagementServiceClient iam)
        {
            var response = await iam.GetUserAsync(new GetUserRequest());
            return (response.User.UserName, response.User.Arn);
        }

        // ---- IPermissionsProvider implementation ----

        public async Task<PolicyListResponse> GetAllPolicies(CloudConnectionSecrets account)
        {
            var iam = GetClient(account);
            var (userName, userArn) = await GetUserInfo(iam);
            var allPolicies = new List<PolicyResponse>();

            // 1. User-attached managed policies
            string? marker = null;
            do
            {
                var resp = await iam.ListAttachedUserPoliciesAsync(new ListAttachedUserPoliciesRequest
                {
                    UserName = userName,
                    Marker = marker
                });

                foreach (var p in resp.AttachedPolicies ?? [])
                {
                    var statements = await FetchManagedPolicyStatements(iam, p.PolicyArn);
                    allPolicies.Add(new PolicyResponse
                    {
                        PolicyArn = p.PolicyArn,
                        PolicyName = p.PolicyName,
                        PolicyType = ResolvePolicyType(p.PolicyArn),
                        AttachedVia = "User",
                        Statements = statements
                    });
                }

                marker = resp.IsTruncated == true ? resp.Marker : null;
            }
            while (marker != null);

            // 2. User inline policies
            marker = null;
            do
            {
                var resp = await iam.ListUserPoliciesAsync(new ListUserPoliciesRequest
                {
                    UserName = userName,
                    Marker = marker
                });

                foreach (var policyName in resp.PolicyNames ?? [])
                {
                    var docResp = await iam.GetUserPolicyAsync(new GetUserPolicyRequest
                    {
                        UserName = userName,
                        PolicyName = policyName
                    });

                    allPolicies.Add(new PolicyResponse
                    {
                        PolicyArn = "Inline",
                        PolicyName = policyName,
                        PolicyType = "Inline",
                        AttachedVia = "User",
                        Statements = ParsePolicyDocument(docResp.PolicyDocument)
                    });
                }

                marker = resp.IsTruncated == true ? resp.Marker : null;
            }
            while (marker != null);

            // 3. Group policies (managed + inline)
            marker = null;
            do
            {
                var groupsResp = await iam.ListGroupsForUserAsync(new ListGroupsForUserRequest
                {
                    UserName = userName,
                    Marker = marker
                });

                foreach (var group in groupsResp.Groups ?? [])
                {
                    // Group managed policies
                    string? gMarker = null;
                    do
                    {
                        var gResp = await iam.ListAttachedGroupPoliciesAsync(
                            new ListAttachedGroupPoliciesRequest
                            {
                                GroupName = group.GroupName,
                                Marker = gMarker
                            });

                        foreach (var p in gResp.AttachedPolicies ?? [])
                        {
                            var statements = await FetchManagedPolicyStatements(iam, p.PolicyArn);
                            allPolicies.Add(new PolicyResponse
                            {
                                PolicyArn = p.PolicyArn,
                                PolicyName = p.PolicyName,
                                PolicyType = ResolvePolicyType(p.PolicyArn),
                                AttachedVia = $"Group: {group.GroupName}",
                                Statements = statements
                            });
                        }

                        gMarker = gResp.IsTruncated == true ? gResp.Marker : null;
                    }
                    while (gMarker != null);

                    // Group inline policies
                    gMarker = null;
                    do
                    {
                        var gInlineResp = await iam.ListGroupPoliciesAsync(
                            new ListGroupPoliciesRequest
                            {
                                GroupName = group.GroupName,
                                Marker = gMarker
                            });

                        foreach (var policyName in gInlineResp.PolicyNames ?? [])
                        {
                            var docResp = await iam.GetGroupPolicyAsync(new GetGroupPolicyRequest
                            {
                                GroupName = group.GroupName,
                                PolicyName = policyName
                            });

                            allPolicies.Add(new PolicyResponse
                            {
                                PolicyArn = "Inline",
                                PolicyName = policyName,
                                PolicyType = "Inline",
                                AttachedVia = $"Group: {group.GroupName}",
                                Statements = ParsePolicyDocument(docResp.PolicyDocument)
                            });
                        }

                        gMarker = gInlineResp.IsTruncated == true ? gInlineResp.Marker : null;
                    }
                    while (gMarker != null);
                }

                marker = groupsResp.IsTruncated == true ? groupsResp.Marker : null;
            }
            while (marker != null);

            return new PolicyListResponse
            {
                UserName = userName,
                UserArn = userArn,
                TotalPolicies = allPolicies.Count,
                Policies = allPolicies
            };
        }

        public async Task<PermissionSummaryResponse> GetSummary(CloudConnectionSecrets account)
        {
            var iam = GetClient(account);
            var (userName, userArn) = await GetUserInfo(iam);
            var attachmentInfos = new List<PolicyAttachmentInfo>();
            var groups = new List<string>();
            int attachedManaged = 0, inlineCount = 0, groupPoliciesCount = 0;

            // Managed policies on user
            string? marker = null;
            do
            {
                var resp = await iam.ListAttachedUserPoliciesAsync(new ListAttachedUserPoliciesRequest
                {
                    UserName = userName,
                    Marker = marker
                });

                foreach (var p in resp.AttachedPolicies ?? [])
                {
                    attachmentInfos.Add(new PolicyAttachmentInfo
                    {
                        PolicyArn = p.PolicyArn,
                        PolicyName = p.PolicyName,
                        Type = ResolvePolicyType(p.PolicyArn),
                        AttachedVia = "User"
                    });
                    attachedManaged++;
                }

                marker = resp.IsTruncated == true ? resp.Marker : null;
            }
            while (marker != null);

            // Inline policies on user
            marker = null;
            do
            {
                var resp = await iam.ListUserPoliciesAsync(new ListUserPoliciesRequest
                {
                    UserName = userName,
                    Marker = marker
                });

                foreach (var name in resp.PolicyNames ?? [])
                {
                    attachmentInfos.Add(new PolicyAttachmentInfo
                    {
                        PolicyArn = "Inline",
                        PolicyName = name,
                        Type = "Inline",
                        AttachedVia = "User"
                    });
                    inlineCount++;
                }

                marker = resp.IsTruncated == true ? resp.Marker : null;
            }
            while (marker != null);

            // Group membership + their policies
            marker = null;
            do
            {
                var groupsResp = await iam.ListGroupsForUserAsync(new ListGroupsForUserRequest
                {
                    UserName = userName,
                    Marker = marker
                });

                foreach (var group in groupsResp.Groups ?? [])
                {
                    groups.Add(group.GroupName);

                    string? gMarker = null;
                    do
                    {
                        var gResp = await iam.ListAttachedGroupPoliciesAsync(
                            new ListAttachedGroupPoliciesRequest
                            {
                                GroupName = group.GroupName,
                                Marker = gMarker
                            });

                        foreach (var p in gResp.AttachedPolicies ?? [])
                        {
                            attachmentInfos.Add(new PolicyAttachmentInfo
                            {
                                PolicyArn = p.PolicyArn,
                                PolicyName = p.PolicyName,
                                Type = ResolvePolicyType(p.PolicyArn),
                                AttachedVia = $"Group: {group.GroupName}"
                            });
                            groupPoliciesCount++;
                        }

                        gMarker = gResp.IsTruncated == true ? gResp.Marker : null;
                    }
                    while (gMarker != null);

                    // Group inline policies
                    gMarker = null;
                    do
                    {
                        var gInlineResp = await iam.ListGroupPoliciesAsync(
                            new ListGroupPoliciesRequest
                            {
                                GroupName = group.GroupName,
                                Marker = gMarker
                            });

                        foreach (var name in gInlineResp.PolicyNames ?? [])
                        {
                            attachmentInfos.Add(new PolicyAttachmentInfo
                            {
                                PolicyArn = "Inline",
                                PolicyName = name,
                                Type = "Inline",
                                AttachedVia = $"Group: {group.GroupName}"
                            });
                            groupPoliciesCount++;
                        }

                        gMarker = gInlineResp.IsTruncated == true ? gInlineResp.Marker : null;
                    }
                    while (gMarker != null);
                }

                marker = groupsResp.IsTruncated == true ? groupsResp.Marker : null;
            }
            while (marker != null);

            return new PermissionSummaryResponse
            {
                UserName = userName,
                UserArn = userArn,
                AttachedManagedPoliciesCount = attachedManaged,
                InlinePoliciesCount = inlineCount,
                GroupPoliciesCount = groupPoliciesCount,
                Groups = groups,
                Policies = attachmentInfos
            };
        }

        public async Task<PermissionCheckResponse> CheckPermissions(
            CloudConnectionSecrets account,
            List<string> actions,
            List<string>? resourceArns)
        {
            var iam = GetClient(account);
            var (_, userArn) = await GetUserInfo(iam);

            var resources = (resourceArns != null && resourceArns.Count > 0)
                ? resourceArns
                : new List<string> { "*" };

            var results = new List<CheckPermissionResult>();
            string? marker = null;

            do
            {
                var resp = await iam.SimulatePrincipalPolicyAsync(new SimulatePrincipalPolicyRequest
                {
                    PolicySourceArn = userArn,
                    ActionNames = actions,
                    ResourceArns = resources,
                    Marker = marker
                });

                foreach (var eval in resp.EvaluationResults ?? [])
                {
                    results.Add(new CheckPermissionResult
                    {
                        Action = eval.EvalActionName,
                        Resource = eval.EvalResourceName,
                        EvalDecision = eval.EvalDecision ?? string.Empty,
                        IsAllowed = eval.EvalDecision == "allowed"
                    });
                }

                marker = resp.IsTruncated == true ? resp.Marker : null;
            }
            while (marker != null);

            return new PermissionCheckResponse
            {
                AllowedCount = results.Count(r => r.IsAllowed),
                DeniedCount = results.Count(r => !r.IsAllowed),
                Results = results
            };
        }

        public async Task AttachPolicy(CloudConnectionSecrets account, string policyArn)
        {
            var iam = GetClient(account);
            var userName = await GetUserName(iam);

            await iam.AttachUserPolicyAsync(new AttachUserPolicyRequest
            {
                UserName = userName,
                PolicyArn = policyArn
            });
        }

        public async Task DetachPolicy(CloudConnectionSecrets account, string policyArn)
        {
            var iam = GetClient(account);
            var userName = await GetUserName(iam);

            await iam.DetachUserPolicyAsync(new DetachUserPolicyRequest
            {
                UserName = userName,
                PolicyArn = policyArn
            });
        }

        public async Task<AvailablePoliciesListResponse> ListAvailablePolicies(
            CloudConnectionSecrets account,
            string scope,
            string? search)
        {
            var iam = GetClient(account);
            var policies = new List<AvailablePolicyResponse>();

            // Determine the IAM scope string
            var iamScope = scope.ToUpperInvariant() switch
            {
                "LOCAL" => "Local",
                "ALL" => "All",
                _ => "AWS"
            };

            string? marker = null;
            const int maxResults = 200;

            do
            {
                var resp = await iam.ListPoliciesAsync(new ListPoliciesRequest
                {
                    Scope = iamScope,
                    OnlyAttached = false,
                    Marker = marker
                });

                foreach (var p in resp.Policies ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(search) &&
                        !p.PolicyName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    policies.Add(new AvailablePolicyResponse
                    {
                        PolicyArn = p.Arn,
                        PolicyName = p.PolicyName,
                        Description = p.Description ?? string.Empty,
                        Scope = iamScope == "AWS" ? "AWS" : "Local"
                    });

                    if (policies.Count >= maxResults) break;
                }

                marker = resp.IsTruncated == true && policies.Count < maxResults ? resp.Marker : null;
            }
            while (marker != null);

            return new AvailablePoliciesListResponse
            {
                TotalCount = policies.Count,
                Policies = policies
            };
        }
    }
}
