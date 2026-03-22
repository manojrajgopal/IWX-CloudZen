using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudAccounts.Entities
{
    public class CloudAccount
    {
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        [Required, MaxLength(32)]
        public string Provider { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string AccessKeyEncrypted { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string SecretKeyEncrypted { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? TenantIdEncrypted { get; set; }

        [MaxLength(2000)]
        public string? ClientIdEncrypted { get; set; }

        [MaxLength(2000)]
        public string? ClientSecretEncrypted { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastValidatedAt { get; set; }
    }
}