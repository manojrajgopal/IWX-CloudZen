using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.SecurityGroups.DTOs;

namespace IWX_CloudZen.CloudServices.SecurityGroups.Interfaces
{
    public interface ISecurityGroupProvider
    {
        /// <summary>Fetches all security groups. Optionally scoped to a single VPC.</summary>
        Task<List<CloudSecurityGroupInfo>> FetchAllSecurityGroups(
            CloudConnectionSecrets account, string? vpcId = null);

        /// <summary>Creates a new security group with optional initial inbound/outbound rules.</summary>
        Task<CloudSecurityGroupInfo> CreateSecurityGroup(
            CloudConnectionSecrets account, CreateSecurityGroupRequest request);

        /// <summary>Updates the Name tag of an existing security group.</summary>
        Task<CloudSecurityGroupInfo> UpdateSecurityGroup(
            CloudConnectionSecrets account, string securityGroupId, UpdateSecurityGroupRequest request);

        /// <summary>Deletes a security group from the cloud.</summary>
        Task DeleteSecurityGroup(CloudConnectionSecrets account, string securityGroupId);

        // ---- Rule management ----

        /// <summary>Adds inbound (ingress) rules to an existing security group.</summary>
        Task<CloudSecurityGroupInfo> AddInboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<SecurityGroupRuleDto> rules);

        /// <summary>Removes inbound (ingress) rules by their rule IDs.</summary>
        Task<CloudSecurityGroupInfo> RemoveInboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<string> ruleIds);

        /// <summary>Adds outbound (egress) rules to an existing security group.</summary>
        Task<CloudSecurityGroupInfo> AddOutboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<SecurityGroupRuleDto> rules);

        /// <summary>Removes outbound (egress) rules by their rule IDs.</summary>
        Task<CloudSecurityGroupInfo> RemoveOutboundRules(
            CloudConnectionSecrets account, string securityGroupId, List<string> ruleIds);
    }
}
