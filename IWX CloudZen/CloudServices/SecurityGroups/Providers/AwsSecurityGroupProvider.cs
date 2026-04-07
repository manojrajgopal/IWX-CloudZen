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

        private static List<DtoSgRule> MapIpPermissions(List<IpPermission>? permissions)
        {
            if (permissions is null) return new();

            return permissions.Select(p => new DtoSgRule
            {
                Protocol = p.IpProtocol ?? "-1",
                FromPort = p.FromPort ?? -1,
                ToPort = p.ToPort ?? -1,
                Ipv4Ranges = p.Ipv4Ranges?.Select(r => r.CidrIp).Where(c => c != null).ToList()!
                             ?? new List<string>(),
                Ipv6Ranges = p.Ipv6Ranges?.Select(r => r.CidrIpv6).Where(c => c != null).ToList()!
                             ?? new List<string>(),
                ReferencedGroupIds = p.UserIdGroupPairs?.Select(g => g.GroupId)
                             .Where(id => id != null).ToList()!
                             ?? new List<string>(),
                Description = p.Ipv4Ranges?.FirstOrDefault()?.Description
                              ?? p.Ipv6Ranges?.FirstOrDefault()?.Description
            }).ToList();
        }

        private static CloudSecurityGroupInfo MapSecurityGroup(SecurityGroup sg) => new()
        {
            SecurityGroupId = sg.GroupId,
            GroupName = sg.GroupName,
            Description = sg.Description ?? string.Empty,
            VpcId = sg.VpcId,
            OwnerId = sg.OwnerId,
            InboundRules = MapIpPermissions(sg.IpPermissions),
            OutboundRules = MapIpPermissions(sg.IpPermissionsEgress)
        };

        private static List<IpPermission> BuildIpPermissions(List<DtoSgRule> rules) =>
            rules.Select(r =>
            {
                var perm = new IpPermission
                {
                    IpProtocol = r.Protocol,
                    FromPort = r.FromPort == -1 ? null : r.FromPort,
                    ToPort = r.ToPort == -1 ? null : r.ToPort
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
            var response = await client.DescribeSecurityGroupsAsync(
                new DescribeSecurityGroupsRequest
                {
                    GroupIds = new List<string> { securityGroupId }
                });

            var sg = response.SecurityGroups.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Security group '{securityGroupId}' not found in AWS.");

            return MapSecurityGroup(sg);
        }

        // ================================================================
        // FetchAll
        // ================================================================

        public async Task<List<CloudSecurityGroupInfo>> FetchAllSecurityGroups(
            CloudConnectionSecrets account, string? vpcId = null)
        {
            var client = GetClient(account);
            var result = new List<CloudSecurityGroupInfo>();
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
                result.AddRange(response.SecurityGroups.Select(MapSecurityGroup));
                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
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
