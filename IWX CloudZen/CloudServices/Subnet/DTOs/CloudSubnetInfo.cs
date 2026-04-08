namespace IWX_CloudZen.CloudServices.Subnet.DTOs
{
    /// <summary>Cloud-side subnet information returned by the provider (sync / create / update).</summary>
    public class CloudSubnetInfo
    {
        public string SubnetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string VpcId { get; set; } = string.Empty;
        public string CidrBlock { get; set; } = string.Empty;
        public string? Ipv6CidrBlock { get; set; }
        public string AvailabilityZone { get; set; } = string.Empty;
        public string? AvailabilityZoneId { get; set; }
        public string State { get; set; } = string.Empty;
        public int AvailableIpAddressCount { get; set; }
        public bool IsDefault { get; set; }
        public bool MapPublicIpOnLaunch { get; set; }
        public bool AssignIpv6AddressOnCreation { get; set; }
    }
}
