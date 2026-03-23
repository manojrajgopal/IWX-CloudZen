namespace IWX_CloudZen.CloudAccounts.DTOs
{
    public class ConnectCloudRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;

        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }

        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }

        public string? Region { get; set; }

        public bool MakeDefault { get; set; }
    }
}