namespace IWX_CloudZen.CloudAccounts.DTOs
{
    public class CloudAccountResponse
    {
        public int Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastValidatedAt { get; set; }
    }
}