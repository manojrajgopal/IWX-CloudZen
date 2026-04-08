namespace IWX_CloudZen.CloudServices.Subnet.DTOs
{
    public class SubnetResponse
    {
        public int Id { get; set; }
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
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SubnetListResponse
    {
        public int TotalCount { get; set; }

        /// <summary>Optional VPC filter applied when listing.</summary>
        public string? VpcIdFilter { get; set; }

        public List<SubnetResponse> Subnets { get; set; } = new();
    }

    public class SyncSubnetResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<SubnetResponse> Subnets { get; set; } = new();
    }
}
