namespace IWX_CloudZen.CloudAccounts.Entities
{
    public class CloudAccount
    {
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string Provider { get; set; }
        public string AccountName { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Region { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
