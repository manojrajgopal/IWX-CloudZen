using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.InternetGateway.Entities
{
    public class InternetGatewayRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string InternetGatewayId { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? AttachedVpcId { get; set; }

        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? OwnerId { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
