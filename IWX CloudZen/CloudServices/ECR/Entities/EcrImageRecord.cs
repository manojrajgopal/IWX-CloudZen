using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.ECR.Entities
{
    public class EcrImageRecord
    {
        public int Id { get; set; }

        public int RepositoryRecordId { get; set; }

        [Required, MaxLength(256)]
        public string RepositoryName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ImageTag { get; set; }

        [MaxLength(100)]
        public string? ImageDigest { get; set; }

        public long SizeInBytes { get; set; }

        /// <summary>"COMPLETE" | "FAILED" | "QUEUED" | "IN_PROGRESS" | "UNSUPPORTED_IMAGE" | null</summary>
        [MaxLength(30)]
        public string? ScanStatus { get; set; }

        public int? FindingsCritical { get; set; }
        public int? FindingsHigh { get; set; }
        public int? FindingsMedium { get; set; }
        public int? FindingsLow { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? PushedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
