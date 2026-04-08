namespace IWX_CloudZen.CloudServices.SecurityGroups.DTOs
{
    /// <summary>Cloud-side security group as returned by the provider during sync or create.</summary>
    public class CloudSecurityGroupInfo
    {
        public string SecurityGroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? VpcId { get; set; }
        public string? OwnerId { get; set; }
        public List<SecurityGroupRuleDto> InboundRules { get; set; } = new();
        public List<SecurityGroupRuleDto> OutboundRules { get; set; } = new();
    }
}
