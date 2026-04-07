namespace IWX_CloudZen.CloudServices.SecurityGroups.DTOs
{
    /// <summary>
    /// A single inbound or outbound security group rule.
    /// Mirrors an AWS IpPermission entry, supporting both IPv4 and IPv6 CIDRs
    /// as well as source/destination security group references.
    /// </summary>
    public class SecurityGroupRuleDto
    {
        /// <summary>Cloud-side rule ID, e.g. sgr-0abc1234 (AWS). Null for new rules.</summary>
        public string? RuleId { get; set; }

        /// <summary>IP protocol: "tcp" | "udp" | "icmp" | "icmpv6" | "-1" (all).</summary>
        public string Protocol { get; set; } = "-1";

        /// <summary>Start of port range. -1 for all / ICMP type.</summary>
        public int FromPort { get; set; } = -1;

        /// <summary>End of port range. -1 for all / ICMP code.</summary>
        public int ToPort { get; set; } = -1;

        /// <summary>IPv4 CIDR ranges this rule applies to, e.g. ["0.0.0.0/0"].</summary>
        public List<string> Ipv4Ranges { get; set; } = new();

        /// <summary>IPv6 CIDR ranges this rule applies to, e.g. ["::/0"].</summary>
        public List<string> Ipv6Ranges { get; set; } = new();

        /// <summary>
        /// Referenced security group IDs (for source/destination SG rules).
        /// Each entry is a group ID, e.g. ["sg-0abc1234"].
        /// </summary>
        public List<string> ReferencedGroupIds { get; set; } = new();

        /// <summary>Optional human-readable description for this specific rule.</summary>
        public string? Description { get; set; }
    }
}
