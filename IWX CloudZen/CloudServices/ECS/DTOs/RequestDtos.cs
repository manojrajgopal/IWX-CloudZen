using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.ECS.DTOs
{
    /// <summary>Request to register a new ECS task definition (or new revision of an existing family).</summary>
    public class RegisterTaskDefinitionRequest
    {
        [Required, MaxLength(200)]
        public string Family { get; set; } = string.Empty;

        /// <summary>CPU units: "256" | "512" | "1024" | "2048" | "4096"</summary>
        public string Cpu { get; set; } = "256";

        /// <summary>Memory in MB. Must be compatible with the selected CPU.</summary>
        public string Memory { get; set; } = "512";

        /// <summary>bridge | host | awsvpc | none</summary>
        public string NetworkMode { get; set; } = "awsvpc";

        public string? ExecutionRoleArn { get; set; }
        public string? TaskRoleArn { get; set; }

        /// <summary>Launch type compatibilities, e.g. ["FARGATE"] or ["EC2"].</summary>
        public List<string> RequiresCompatibilities { get; set; } = new() { "FARGATE" };

        /// <summary>LINUX | WINDOWS_SERVER_2019_FULL | WINDOWS_SERVER_2022_FULL</summary>
        public string OsFamily { get; set; } = "LINUX";

        [MinLength(1, ErrorMessage = "At least one container definition is required.")]
        public List<ContainerDefinitionDto> ContainerDefinitions { get; set; } = new();
    }

    /// <summary>Request to create a new ECS service inside a cluster.</summary>
    public class CreateEcsServiceRequest
    {
        [Required, MaxLength(256)]
        public string ServiceName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string ClusterName { get; set; } = string.Empty;

        /// <summary>Task definition in "family:revision" or ARN format.</summary>
        [Required]
        public string TaskDefinition { get; set; } = string.Empty;

        public int DesiredCount { get; set; } = 1;

        /// <summary>FARGATE | EC2 | EXTERNAL</summary>
        public string LaunchType { get; set; } = "FARGATE";

        /// <summary>REPLICA | DAEMON</summary>
        public string SchedulingStrategy { get; set; } = "REPLICA";

        /// <summary>Required for FARGATE launch type.</summary>
        public NetworkConfigurationDto? NetworkConfiguration { get; set; }
    }

    /// <summary>Request to update an existing ECS service (desired count and/or task definition).</summary>
    public class UpdateEcsServiceRequest
    {
        public int? DesiredCount { get; set; }

        /// <summary>New task definition in "family:revision" or ARN format.</summary>
        public string? TaskDefinition { get; set; }
    }

    /// <summary>Request to run a one-off ECS task.</summary>
    public class RunTaskRequest
    {
        [Required, MaxLength(256)]
        public string ClusterName { get; set; } = string.Empty;

        /// <summary>Task definition in "family:revision" or ARN format.</summary>
        [Required]
        public string TaskDefinition { get; set; } = string.Empty;

        /// <summary>FARGATE | EC2</summary>
        public string LaunchType { get; set; } = "FARGATE";

        [Range(1, 10, ErrorMessage = "Count must be between 1 and 10.")]
        public int Count { get; set; } = 1;

        /// <summary>Required for FARGATE launch type.</summary>
        public NetworkConfigurationDto? NetworkConfiguration { get; set; }

        /// <summary>Optional per-container environment variable overrides at runtime.</summary>
        public List<ContainerEnvironmentOverrideDto> EnvironmentOverrides { get; set; } = new();
    }

    /// <summary>Request to stop a running ECS task.</summary>
    public class StopTaskRequest
    {
        /// <summary>Optional human-readable reason recorded in task history.</summary>
        [MaxLength(255)]
        public string? Reason { get; set; }
    }
}
