namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class Ec2InstanceConnectEndpointResponse
    {
        public int Id { get; set; }
        public string EndpointId { get; set; } = string.Empty;
        public string SubnetId { get; set; } = string.Empty;
        public string VpcId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string DnsName { get; set; } = string.Empty;
        public string NetworkInterfaceId { get; set; } = string.Empty;
        public string AvailabilityZone { get; set; } = string.Empty;
        public string FipsDnsName { get; set; } = string.Empty;
        public bool PreserveClientIp { get; set; }
        public List<string> SecurityGroupIds { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
