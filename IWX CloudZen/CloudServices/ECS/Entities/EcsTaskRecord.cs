using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.ECS.Entities
{
    public class EcsTaskRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string TaskArn { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string ClusterName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ClusterArn { get; set; }

        [MaxLength(500)]
        public string? TaskDefinitionArn { get; set; }

        /// <summary>service:name | started-by value</summary>
        [MaxLength(256)]
        public string? Group { get; set; }

        /// <summary>PROVISIONING | PENDING | ACTIVATING | RUNNING | DEACTIVATING | STOPPING | DEPROVISIONING | STOPPED</summary>
        [MaxLength(30)]
        public string LastStatus { get; set; } = string.Empty;

        /// <summary>RUNNING | STOPPED | PENDING</summary>
        [MaxLength(30)]
        public string DesiredStatus { get; set; } = string.Empty;

        /// <summary>CPU units assigned to this task</summary>
        [MaxLength(10)]
        public string? Cpu { get; set; }

        /// <summary>Memory in MB assigned to this task</summary>
        [MaxLength(10)]
        public string? Memory { get; set; }

        /// <summary>FARGATE | EC2</summary>
        [MaxLength(20)]
        public string LaunchType { get; set; } = "FARGATE";

        /// <summary>CONNECTED | DISCONNECTED</summary>
        [MaxLength(20)]
        public string? Connectivity { get; set; }

        [MaxLength(100)]
        public string? StopCode { get; set; }

        [MaxLength(1000)]
        public string? StoppedReason { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? PullStartedAt { get; set; }
        public DateTime? PullStoppedAt { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
