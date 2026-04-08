using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.CloudWatchLogs.Entities
{
    public class LogGroupRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(512)]
        public string LogGroupName { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? Arn { get; set; }

        public int? RetentionInDays { get; set; }

        public long StoredBytes { get; set; }

        [MaxLength(50)]
        public string MetricFilterCount { get; set; } = "0";

        [MaxLength(50)]
        public string? KmsKeyId { get; set; }

        [MaxLength(50)]
        public string? DataProtectionStatus { get; set; }

        [MaxLength(50)]
        public string? LogGroupClass { get; set; }

        public DateTime? CreationTimeUtc { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class LogStreamRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(512)]
        public string LogStreamName { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? Arn { get; set; }

        [Required, MaxLength(512)]
        public string LogGroupName { get; set; } = string.Empty;

        public int LogGroupRecordId { get; set; }

        public DateTime? FirstEventTimestamp { get; set; }

        public DateTime? LastEventTimestamp { get; set; }

        public DateTime? LastIngestionTime { get; set; }

        public long StoredBytes { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
