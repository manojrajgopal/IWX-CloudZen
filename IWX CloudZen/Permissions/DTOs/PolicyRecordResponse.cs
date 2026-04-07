namespace IWX_CloudZen.Permissions.DTOs
{
    public class PolicyRecordResponse
    {
        public int Id { get; set; }
        public string PolicyArn { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public string PolicyType { get; set; } = string.Empty;
        public string AttachedVia { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
