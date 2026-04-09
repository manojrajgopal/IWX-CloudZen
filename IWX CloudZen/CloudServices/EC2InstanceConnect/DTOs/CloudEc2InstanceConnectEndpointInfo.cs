namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class CloudEc2InstanceConnectEndpointInfo
    {
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
    }
}
