namespace IWX_CloudZen.CloudServices.InternetGateway.DTOs
{
    public class InternetGatewayResponse
    {
        public int Id { get; set; }
        public string InternetGatewayId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AttachedVpcId { get; set; }
        public string State { get; set; } = string.Empty;
        public string? OwnerId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
