using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.Subnet.DTOs
{
    /// <summary>Request to create a new subnet inside a VPC.</summary>
    public class CreateSubnetRequest
    {
        /// <summary>Human-readable name (stored as the AWS Name tag).</summary>
        [Required, MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        /// <summary>The VPC to create the subnet in, e.g. vpc-0abc1234.</summary>
        [Required, MaxLength(100)]
        public string VpcId { get; set; } = string.Empty;

        /// <summary>IPv4 CIDR block for the subnet, e.g. 10.0.1.0/24.</summary>
        [Required, MaxLength(50)]
        public string CidrBlock { get; set; } = string.Empty;

        /// <summary>Availability Zone name, e.g. us-east-1a.</summary>
        [Required, MaxLength(50)]
        public string AvailabilityZone { get; set; } = string.Empty;

        /// <summary>Whether instances launched here automatically receive a public IPv4 address.</summary>
        public bool MapPublicIpOnLaunch { get; set; } = false;

        /// <summary>Optional IPv6 CIDR block (must be within the VPC's IPv6 CIDR, if any).</summary>
        [MaxLength(60)]
        public string? Ipv6CidrBlock { get; set; }
    }

    /// <summary>Request to update a subnet's mutable settings.</summary>
    public class UpdateSubnetRequest
    {
        /// <summary>New human-readable name (updates the AWS Name tag).</summary>
        [MaxLength(256)]
        public string? Name { get; set; }

        /// <summary>Enable or disable automatic public IPv4 assignment on launch.</summary>
        public bool? MapPublicIpOnLaunch { get; set; }

        /// <summary>Enable or disable automatic public IPv6 address assignment on launch.</summary>
        public bool? AssignIpv6AddressOnCreation { get; set; }
    }
}
