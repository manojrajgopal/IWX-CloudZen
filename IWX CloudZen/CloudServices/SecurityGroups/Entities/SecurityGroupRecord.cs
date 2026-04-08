using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.SecurityGroups.Entities
{
    /// <summary>Represents a cloud security group stored in the local database.</summary>
    public class SecurityGroupRecord
    {
        public int Id { get; set; }

        /// <summary>Cloud-provider security group ID, e.g. sg-0abc1234.</summary>
        [Required, MaxLength(100)]
        public string SecurityGroupId { get; set; } = string.Empty;

        /// <summary>Security group name as set on the cloud provider.</summary>
        [Required, MaxLength(256)]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Human-readable description.</summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>The VPC this security group belongs to, e.g. vpc-0abc1234.</summary>
        [MaxLength(100)]
        public string? VpcId { get; set; }

        /// <summary>AWS account / owner ID of the security group.</summary>
        [MaxLength(100)]
        public string? OwnerId { get; set; }

        /// <summary>
        /// Inbound (ingress) rules as a JSON array of SecurityGroupRuleDto.
        /// Stored as JSON for flexibility; reflects current AWS state after sync or mutation.
        /// </summary>
        public string? InboundRulesJson { get; set; }

        /// <summary>
        /// Outbound (egress) rules as a JSON array of SecurityGroupRuleDto.
        /// </summary>
        public string? OutboundRulesJson { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
