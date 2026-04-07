using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.Subnet.Entities
{
    public class SubnetRecord
    {
        public int Id { get; set; }

        /// <summary>The cloud-provider subnet ID, e.g. subnet-0abc1234.</summary>
        [Required, MaxLength(100)]
        public string SubnetId { get; set; } = string.Empty;

        /// <summary>Human-readable name (from the Name tag on AWS).</summary>
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        /// <summary>The VPC this subnet belongs to, e.g. vpc-0abc1234.</summary>
        [Required, MaxLength(100)]
        public string VpcId { get; set; } = string.Empty;

        /// <summary>IPv4 CIDR block, e.g. 10.0.1.0/24.</summary>
        [Required, MaxLength(50)]
        public string CidrBlock { get; set; } = string.Empty;

        /// <summary>IPv6 CIDR block when assigned, e.g. 2001:db8::/64.</summary>
        [MaxLength(60)]
        public string? Ipv6CidrBlock { get; set; }

        /// <summary>Availability Zone name, e.g. us-east-1a.</summary>
        [MaxLength(50)]
        public string AvailabilityZone { get; set; } = string.Empty;

        /// <summary>Availability Zone ID, e.g. use1-az1.</summary>
        [MaxLength(50)]
        public string? AvailabilityZoneId { get; set; }

        /// <summary>available | pending | unavailable</summary>
        [MaxLength(30)]
        public string State { get; set; } = string.Empty;

        /// <summary>Number of available private IPv4 addresses in the subnet.</summary>
        public int AvailableIpAddressCount { get; set; }

        public bool IsDefault { get; set; }

        /// <summary>Whether instances launched here receive a public IPv4 address by default.</summary>
        public bool MapPublicIpOnLaunch { get; set; }

        /// <summary>Whether instances receive a public IPv6 address by default.</summary>
        public bool AssignIpv6AddressOnCreation { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
