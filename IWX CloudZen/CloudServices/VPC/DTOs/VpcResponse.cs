namespace IWX_CloudZen.CloudServices.VPC.DTOs
{
    public class VpcResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VpcId { get; set; } = string.Empty;
        public string CidrBlock { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool EnableDnsSupport { get; set; }
        public bool EnableDnsHostnames { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
