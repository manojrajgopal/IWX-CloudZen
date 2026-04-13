using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.SecurityGroups.DTOs;
using IWX_CloudZen.CloudServices.SecurityGroups.Interfaces;
using System.Text.Json;

// Disambiguate conflicting names
using DtoCreateSg  = IWX_CloudZen.CloudServices.SecurityGroups.DTOs.CreateSecurityGroupRequest;
using DtoUpdateSg  = IWX_CloudZen.CloudServices.SecurityGroups.DTOs.UpdateSecurityGroupRequest;
using DtoSgRule    = IWX_CloudZen.CloudServices.SecurityGroups.DTOs.SecurityGroupRuleDto;

namespace IWX_CloudZen.CloudServices.SecurityGroups.Providers
{
    public class AwsSecurityGroupProvider : ISecurityGroupProvider
    {
        // ================================================================
        // Client
        // ================================================================

        private static AmazonEC2Client GetClient(CloudConnectionSecrets account) =>
            new AmazonEC2Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));

        // ================================================================
        // Helpers / Mappers
        // ================================================================

        private static string GetNameTag(List<Tag>? tags) =>
            tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty;

        // Maps a single SecurityGroupRule (from DescribeSecurityGroupRules) — includes RuleId.
        private static DtoSgRule MapRule(SecurityGroupRule r) => new()
        {
            RuleId      = r.SecurityGroupRuleId,
            Protocol    = r.IpProtocol ?? "-1",
            FromPort    = r.FromPort ?? -1,
            ToPort      = r.ToPort ?? -1,
            Ipv4Ranges  = !string.IsNullOrEmpty(r.CidrIpv4)
                            ? new List<string> { r.CidrIpv4 }
                            : new(),
            Ipv6Ranges  = !string.IsNullOrEmpty(r.CidrIpv6)
                            ? new List<string> { r.CidrIpv6 }
                            : new(),
            ReferencedGroupIds = r.ReferencedGroupInfo?.GroupId is not null
                            ? new List<string> { r.ReferencedGroupInfo.GroupId }
                            : new(),
            Description = r.Description
        };

        // Fetches all rules for a batch of group IDs in one paginated call.
        // Returns dict[groupId] -> (inboundRules, outboundRules).
        private static async Task<Dictionary<string, (List<DtoSgRule> In, List<DtoSgRule> Out)>>
            FetchRulesForGroups(AmazonEC2Client client, IReadOnlyList<string> groupIds)
        {
            var map = groupIds.ToDictionary(id => id, _ => (In: new List<DtoSgRule>(), Out: new List<DtoSgRule>()));

            if (groupIds.Count == 0) return map;

            // Process in batches of 200 (AWS filter value limit)
            const int batchSize = 200;
            for (int i = 0; i < groupIds.Count; i += batchSize)
            {
                var batch = groupIds.Skip(i).Take(batchSize).ToList();
                var rulesRequest = new DescribeSecurityGroupRulesRequest
                {
                    Filters = new List<Filter>
                    {
                        new Filter { Name = "group-id", Values = batch }
                    }
                };

                string? rulesToken = null;
                do
                {
                    rulesRequest.NextToken = rulesToken;
                    var rulesResponse = await client.DescribeSecurityGroupRulesAsync(rulesRequest);

                    foreach (var rule in rulesResponse.SecurityGroupRules ?? new())
                    {
                        if (rule.GroupId is null) continue;
                        if (!map.TryGetValue(rule.GroupId, out var lists)) continue;
                        var dto = MapRule(rule);
                        if (rule.IsEgress == true)
                            lists.Out.Add(dto);
                        else
                            lists.In.Add(dto);
                    }

                    rulesToken = rulesResponse.NextToken;
                }
                while (rulesToken != null);
            }

            return map;
        }

        private static CloudSecurityGroupInfo BuildCloudInfo(
            SecurityGroup sg,
            List<DtoSgRule> inbound,
            List<DtoSgRule> outbound)
        {
            var nameTag = GetNameTag(sg.Tags);
            return new CloudSecurityGroupInfo
            {
                SecurityGroupId = sg.GroupId,
                GroupName       = !string.IsNullOrWhiteSpace(nameTag) ? nameTag : sg.GroupName,
                Description     = sg.Description ?? string.Empty,
                VpcId           = sg.VpcId,
                OwnerId         = sg.OwnerId,
                InboundRules    = inbound,
                OutboundRules   = outbound
            };
        }

        private static List<IpPermission> BuildIpPermissions(List<DtoSgRule> rules) =>
            rules.Select(r =>
            {
                var perm = new IpPermission
                {
                    IpProtocol = r.Protocol,
                    FromPort = r.FromPort == -1 ? null : r.FromPort,
                    ToPort   = r.ToPort   == -1 ? null : r.ToPort
                };

                if (r.Ipv4Ranges?.Count > 0)
                    perm.Ipv4Ranges = r.Ipv4Ranges
                        .Select(cidr => new IpRange { CidrIp = cidr, Description = r.Description })
                        .ToList();

                if (r.Ipv6Ranges?.Count > 0)
                    perm.Ipv6Ranges = r.Ipv6Ranges
                        .Select(cidr => new Ipv6Range { CidrIpv6 = cidr, Description = r.Description })
                        .ToList();

                if (r.ReferencedGroupIds?.Count > 0)
                    perm.UserIdGroupPairs = r.ReferencedGroupIds
                        .Select(groupId => new UserIdGroupPair { GroupId = groupId })
                        .ToList();

                return perm;
            }).ToList();

        private async Task<CloudSecurityGroupInfo> DescribeOne(
            AmazonEC2Client client, string securityGroupId)
        {
            var sgResponse = await client.DescribeSecurityGroupsAsync(
                new DescribeSecurityGroupsRequest
                {
                    GroupIds = new List<string> { securityGroupId }
                });

            var sg = sgResponse.SecurityGroups.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Security group '{securityGroupId}' not found in AWS.");

            var rulesMap = await FetchRulesForGroups(client, new[] { securityGroupId });
            var (inbound, outbound) = rulesMap[securityGroupId];

            return BuildCloudInfo(sg, inbound, outbound);
        }

        // ================================================================
        // FetchAll
        // ================================================================

        public async Task<List<CloudSecurityGroupInfo>> FetchAllSecurityGroups(
            CloudConnectionSecrets account, string? vpcId = null)
        {
            var client = GetClient(account);
            var allGroups = new List<SecurityGroup>();
            string? nextToken = null;

            var request = new DescribeSecurityGroupsRequest();

            if (!string.IsNullOrWhiteSpace(vpcId))
            {
                request.Filters = new List<Filter>
                {
                    new Filter { Name = "vpc-id", Values = new List<string> { vpcId } }
                };
            }

            do
            {
                request.NextToken = nextToken;
                var response = await client.DescribeSecurityGroupsAsync(request);
                allGroups.AddRange(response.SecurityGroups);
                nextToken = response.NextToken;
            }
            while (nextToken != null);

            if (allGroups.Count == 0) return new();

            // Fetch all rules in a single batched call to get ruleIds (sgr-xxx)
            var groupIds = allGroups.Select(g => g.GroupId).ToList();
            var rulesMap = await FetchRulesForGroups(client, groupIds);

            return allGroups.Select(sg =>
            {
                var (inbound, outbound) = rulesMap.TryGetValue(sg.GroupId, out var r)
                    ? r : (new List<DtoSgRule>(), new List<DtoSgRule>());
                return BuildCloudInfo(sg, inbound, outbound);
            }).ToList();
        }

        // ================================================================
        // Create
        // ================================================================

        public async Task<CloudSecurityGroupInfo> CreateSecurityGroup(
            CloudConnectionSecrets account, DtoCreateSg request)
        {
            var client = GetClient(account);

            var awsRequest = new Amazon.EC2.Model.CreateSecurityGroupRequest
            {
                GroupName = request.GroupName,
                Description = request.Description
            };

            if (!string.IsNullOrWhiteSpace(request.VpcId))
                awsRequest.VpcId = request.VpcId;

            var createResponse = await client.CreateSecurityGroupAsync(awsRequest);
            var groupId = createResponse.GroupId;

            // Tag with the GroupName as the Name tag for visibility
            await client.CreateTagsAsync(new CreateTagsRequest
            {
                Resources = new List<string> { groupId },
                Tags = new List<Tag> { new Tag { Key = "Name", Value = request.GroupName } }
            });

            // Add inbound rules if provided
            if (request.InboundRules?.Count > 0)
            {
                var perms = BuildIpPermissions(request.InboundRules);
                await client.AuthorizeSecurityGroupIngressAsync(
                    new AuthorizeSecurityGroupIngressRequest
                    {
                        GroupId = groupId,
                        IpPermissions = perms
                    });
            }

            // Override outbound rules if provided (remove AWS default allow-all, then add custom)
            if (request.OutboundRules?.Count > 0)
            {
                // Revoke the default allow-all egress rule first
                await client.RevokeSecurityGroupEgressAsync(
                    new RevokeSecurityGroupEgressRequest
                    {
                        GroupId = groupId,
                        IpPermissions = new List<IpPermission>
                        {
                            new IpPermission
                            {
                                IpProtocol = "-1",
                                Ipv4Ranges = new List<IpRange>
                                {
                                    new IpRange { CidrIp = "0.0.0.0/0" }
                                }
                            }
                        }
                    });

                var perms = BuildIpPermissions(request.OutboundRules);
                await client.AuthorizeSecurityGroupEgressAsync(
                    new AuthorizeSecurityGroupEgressRequest
                    {
                        GroupId = groupId,
                        IpPermissions = perms
                    });
            }

            return await DescribeOne(client, groupId);
        }

        // ================================================================
        // Update
        // ================================================================

        public async Task<CloudSecurityGroupInfo> UpdateSecurityGroup(
            CloudConnectionSecrets account, string securityGroupId, DtoUpdateSg request)
        {
            if (string.IsNullOrWhiteSpace(securityGroupId))
                throw new InvalidOperationException(
                    "Security group AWS ID is missing. Please run Sync to refresh the record before updating.");

            var client = GetClient(account);

            // AWS does not allow renaming the GroupName after creation.
            // We update the "Name" tag instead, which is the display name in the console.
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                await client.CreateTagsAsync(new CreateTagsRequest
                {
                    Resources = new List<string> { securityGroupId },
                    Tags = new List<Tag> { new Tag { Key = "Name", Value = request.Name } }
                });
            }

            return await DescribeOne(client, securityGroupId);
        }

        // ================================================================
        // Delete
        // ================================================================

        public async Task DeleteSecurityGroup(
            CloudConnectionSecrets account, string securityGroupId)
        {
            var client = GetClient(account);

            // AWS does not allow deleting the default security group.
            var describeResponse = await client.DescribeSecurityGroupsAsync(
                new DescribeSecurityGroupsRequest
                {
                    GroupIds = new List<string> { securityGroupId }
                });

            var sg = describeResponse.SecurityGroups?.FirstOrDefault();
            if (sg is not null && string.Equals(sg.GroupName, "default", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"The default security group '{securityGroupId}' cannot be deleted. AWS does not allow deletion of default security groups.");

            // Check for network interfaces still using this security group.
            var eniResponse = await client.DescribeNetworkInterfacesAsync(
                new DescribeNetworkInterfacesRequest
                {
                    Filters = new List<Filter>
                    {
                        new Filter { Name = "group-id", Values = new List<string> { securityGroupId } }
                    }
                });

            var dependentEnis = eniResponse.NetworkInterfaces ?? new();
            if (dependentEnis.Count > 0)
            {
                var details = dependentEnis.Select(eni =>
                {
                    var attachment = eni.Attachment;
                    var owner = attachment?.InstanceId ?? attachment?.InstanceOwnerId ?? eni.Description ?? "unknown";
                    return $"{eni.NetworkInterfaceId} (used by: {owner})";
                });

                throw new InvalidOperationException(
                    $"Security group '{securityGroupId}' cannot be deleted because it is still associated with the following network interfaces: {string.Join(", ", details)}. " +
                    $"Please remove the security group from these resources first.");
            }

            // Remove inbound/outbound rules from OTHER security groups that reference this one.
            var rulesResponse = await client.DescribeSecurityGroupRulesAsync(
                new DescribeSecurityGroupRulesRequest
                {
                    Filters = new List<Filter>
                    {
                        new Filter { Name = "group-id", Values = new List<string> { securityGroupId } }
                    }
                });

            var ingressRuleIds = (rulesResponse.SecurityGroupRules ?? new())
                .Where(r => r.IsEgress == false)
                .Select(r => r.SecurityGroupRuleId)
                .Where(id => id is not null)
                .ToList();

            var egressRuleIds = (rulesResponse.SecurityGroupRules ?? new())
                .Where(r => r.IsEgress == true)
                .Select(r => r.SecurityGroupRuleId)
                .Where(id => id is not null)
                .ToList();

            if (ingressRuleIds.Count > 0)
            {
                await client.RevokeSecurityGroupIngressAsync(
                    new RevokeSecurityGroupIngressRequest
                    {
                        GroupId = securityGroupId,
                        SecurityGroupRuleIds = ingressRuleIds
                    });
            }

            if (egressRuleIds.Count > 0)
            {
                await client.RevokeSecurityGroupEgressAsync(
                    new RevokeSecurityGroupEgressRequest
                    {
                        GroupId = securityGroupId,
                        SecurityGroupRuleIds = egressRuleIds
                    });
            }

            await client.DeleteSecurityGroupAsync(new DeleteSecurityGroupRequest
            {
                GroupId = securityGroupId
            });
        }

        // ================================================================
        // Inbound Rules
        // ================================================================

        public async Task<CloudSecurityGroupInfo> AddInboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<DtoSgRule> rules)
        {
            var client = GetClient(account);

            await client.AuthorizeSecurityGroupIngressAsync(
                new AuthorizeSecurityGroupIngressRequest
                {
                    GroupId = securityGroupId,
                    IpPermissions = BuildIpPermissions(rules)
                });

            return await DescribeOne(client, securityGroupId);
        }

        public async Task<CloudSecurityGroupInfo> RemoveInboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<string> ruleIds)
        {
            var client = GetClient(account);

            await client.RevokeSecurityGroupIngressAsync(
                new RevokeSecurityGroupIngressRequest
                {
                    GroupId = securityGroupId,
                    SecurityGroupRuleIds = ruleIds
                });

            return await DescribeOne(client, securityGroupId);
        }

        // ================================================================
        // Outbound Rules
        // ================================================================

        public async Task<CloudSecurityGroupInfo> AddOutboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<DtoSgRule> rules)
        {
            var client = GetClient(account);

            await client.AuthorizeSecurityGroupEgressAsync(
                new AuthorizeSecurityGroupEgressRequest
                {
                    GroupId = securityGroupId,
                    IpPermissions = BuildIpPermissions(rules)
                });

            return await DescribeOne(client, securityGroupId);
        }

        public async Task<CloudSecurityGroupInfo> RemoveOutboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<string> ruleIds)
        {
            var client = GetClient(account);

            await client.RevokeSecurityGroupEgressAsync(
                new RevokeSecurityGroupEgressRequest
                {
                    GroupId = securityGroupId,
                    SecurityGroupRuleIds = ruleIds
                });

            return await DescribeOne(client, securityGroupId);
        }
    }
}
