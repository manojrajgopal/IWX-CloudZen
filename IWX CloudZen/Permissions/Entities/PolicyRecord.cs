using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.Permissions.Entities
{
    public class PolicyRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string PolicyArn { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string PolicyName { get; set; } = string.Empty;

        /// <summary>"AWS Managed" | "Customer Managed" | "Inline"</summary>
        [Required, MaxLength(30)]
        public string PolicyType { get; set; } = string.Empty;

        /// <summary>"User" | "Group: {GroupName}"</summary>
        [Required, MaxLength(200)]
        public string AttachedVia { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
