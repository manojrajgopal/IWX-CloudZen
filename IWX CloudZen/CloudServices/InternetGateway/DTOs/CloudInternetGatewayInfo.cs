namespace IWX_CloudZen.CloudServices.InternetGateway.DTOs
{
    public class CloudInternetGatewayInfo
    {
        public string InternetGatewayId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AttachedVpcId { get; set; }
        public string State { get; set; } = string.Empty;
        public string? OwnerId { get; set; }
    }
}
