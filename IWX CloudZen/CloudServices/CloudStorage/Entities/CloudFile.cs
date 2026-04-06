using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.CloudStorage.Entities
{
    public class CloudFile
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string FileUrl { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string BucketName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Folder { get; set; } = string.Empty;

        public long Size { get; set; }

        [MaxLength(200)]
        public string ContentType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ETag { get; set; }

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
