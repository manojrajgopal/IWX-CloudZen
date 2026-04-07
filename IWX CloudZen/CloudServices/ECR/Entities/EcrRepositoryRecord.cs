using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.ECR.Entities
{
    public class EcrRepositoryRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string RepositoryName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RepositoryArn { get; set; }

        [MaxLength(512)]
        public string? RepositoryUri { get; set; }

        [MaxLength(50)]
        public string ImageTagMutability { get; set; } = "MUTABLE";

        public bool ScanOnPush { get; set; }

        /// <summary>"ENABLED" | "DISABLED"</summary>
        [MaxLength(20)]
        public string EncryptionType { get; set; } = "AES256";

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
