using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.VPC.Entities
{
    public class VpcRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string VpcId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string CidrBlock { get; set; } = string.Empty;

        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public bool EnableDnsSupport { get; set; }

        public bool EnableDnsHostnames { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
