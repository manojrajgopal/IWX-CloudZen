using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.Cluster.Entities
{
    public class ClusterRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ClusterArn { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        public bool ContainerInsightsEnabled { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
