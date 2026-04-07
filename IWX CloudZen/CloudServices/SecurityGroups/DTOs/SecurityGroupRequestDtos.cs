using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.SecurityGroups.DTOs
{
    /// <summary>Request to create a new security group.</summary>
    public class CreateSecurityGroupRequest
    {
        /// <summary>Security group name. Must be unique within the VPC.</summary>
        [Required, MaxLength(256)]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Human-readable description (required by AWS).</summary>
        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>The VPC to associate this security group with. Required for non-default VPC.</summary>
        [MaxLength(100)]
        public string? VpcId { get; set; }

        /// <summary>Optional inbound rules to add immediately after creation.</summary>
        public List<SecurityGroupRuleDto> InboundRules { get; set; } = new();

        /// <summary>
        /// Optional outbound rules to replace the default "allow all" egress rule.
        /// If empty, AWS default (allow all outbound) is kept.
        /// </summary>
        public List<SecurityGroupRuleDto> OutboundRules { get; set; } = new();
    }

    /// <summary>Request to update the name/description of an existing security group.</summary>
    public class UpdateSecurityGroupRequest
    {
        /// <summary>New Name tag value (AWS Name tag — the group name itself is immutable in AWS).</summary>
        [MaxLength(256)]
        public string? Name { get; set; }

        /// <summary>New description (not supported for update by AWS — included for future providers).</summary>
        [MaxLength(500)]
        public string? Description { get; set; }
    }

    /// <summary>Request to add inbound rules to a security group.</summary>
    public class AddInboundRulesRequest
    {
        [MinLength(1, ErrorMessage = "At least one rule is required.")]
        public List<SecurityGroupRuleDto> Rules { get; set; } = new();
    }

    /// <summary>Request to add outbound rules to a security group.</summary>
    public class AddOutboundRulesRequest
    {
        [MinLength(1, ErrorMessage = "At least one rule is required.")]
        public List<SecurityGroupRuleDto> Rules { get; set; } = new();
    }

    /// <summary>Request to remove specific inbound rules by their rule IDs.</summary>
    public class RemoveInboundRulesRequest
    {
        [MinLength(1, ErrorMessage = "At least one rule ID is required.")]
        public List<string> RuleIds { get; set; } = new();
    }

    /// <summary>Request to remove specific outbound rules by their rule IDs.</summary>
    public class RemoveOutboundRulesRequest
    {
        [MinLength(1, ErrorMessage = "At least one rule ID is required.")]
        public List<string> RuleIds { get; set; } = new();
    }
}
