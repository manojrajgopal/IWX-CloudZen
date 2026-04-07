using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.ECS.Entities
{
    public class EcsTaskDefinitionRecord
    {
        public int Id { get; set; }

        /// <summary>Task definition family name (e.g. "my-app")</summary>
        [Required, MaxLength(200)]
        public string Family { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? TaskDefinitionArn { get; set; }

        public int Revision { get; set; }

        /// <summary>ACTIVE | INACTIVE | DELETE_IN_PROGRESS</summary>
        [MaxLength(30)]
        public string Status { get; set; } = "ACTIVE";

        /// <summary>CPU units: 256 | 512 | 1024 | 2048 | 4096</summary>
        [MaxLength(10)]
        public string Cpu { get; set; } = "256";

        /// <summary>Memory in MB: 512 | 1024 | 2048 | ...</summary>
        [MaxLength(10)]
        public string Memory { get; set; } = "512";

        /// <summary>bridge | host | awsvpc | none</summary>
        [MaxLength(20)]
        public string NetworkMode { get; set; } = "awsvpc";

        [MaxLength(500)]
        public string? ExecutionRoleArn { get; set; }

        [MaxLength(500)]
        public string? TaskRoleArn { get; set; }

        /// <summary>Comma-separated list: FARGATE,EC2</summary>
        [MaxLength(50)]
        public string RequiresCompatibilities { get; set; } = "FARGATE";

        /// <summary>LINUX | WINDOWS_SERVER_2019_FULL | WINDOWS_SERVER_2022_FULL | etc.</summary>
        [MaxLength(60)]
        public string? OsFamily { get; set; }

        /// <summary>Container definitions serialized as JSON</summary>
        public string? ContainerDefinitionsJson { get; set; }

        public int ContainerCount { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
