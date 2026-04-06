namespace IWX_CloudZen.CloudServices.VPC.DTOs
{
    public class CloudVpcInfo
    {
        public string VpcId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CidrBlock { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool EnableDnsSupport { get; set; }
        public bool EnableDnsHostnames { get; set; }
    }
}
