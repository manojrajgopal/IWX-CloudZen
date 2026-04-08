namespace IWX_CloudZen.CloudServices.SecurityGroups.DTOs
{
    public class SecurityGroupResponse
    {
        public int Id { get; set; }
        public string SecurityGroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? VpcId { get; set; }
        public string? OwnerId { get; set; }
        public List<SecurityGroupRuleDto> InboundRules { get; set; } = new();
        public List<SecurityGroupRuleDto> OutboundRules { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SecurityGroupListResponse
    {
        public int TotalCount { get; set; }

        /// <summary>Optional VPC filter applied when listing.</summary>
        public string? VpcIdFilter { get; set; }

        public List<SecurityGroupResponse> SecurityGroups { get; set; } = new();
    }

    public class SyncSecurityGroupResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<SecurityGroupResponse> SecurityGroups { get; set; } = new();
    }
}
