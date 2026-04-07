using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.ECS.Entities
{
    public class EcsServiceRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string ServiceName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ServiceArn { get; set; }

        [Required, MaxLength(256)]
        public string ClusterName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ClusterArn { get; set; }

        /// <summary>Fully qualified task definition, e.g. "family:revision"</summary>
        [MaxLength(300)]
        public string? TaskDefinition { get; set; }

        public int DesiredCount { get; set; }
        public int RunningCount { get; set; }
        public int PendingCount { get; set; }

        /// <summary>ACTIVE | DRAINING | INACTIVE</summary>
        [MaxLength(30)]
        public string Status { get; set; } = string.Empty;

        /// <summary>FARGATE | EC2 | EXTERNAL</summary>
        [MaxLength(20)]
        public string LaunchType { get; set; } = "FARGATE";

        /// <summary>REPLICA | DAEMON</summary>
        [MaxLength(20)]
        public string SchedulingStrategy { get; set; } = "REPLICA";

        /// <summary>AWS VPC network configuration serialized as JSON</summary>
        public string? NetworkConfigurationJson { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? ServiceCreatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
