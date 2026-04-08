namespace IWX_CloudZen.CloudServices.ECS.DTOs
{
    /// <summary>Cloud-side task definition returned by the provider during sync or creation.</summary>
    public class CloudTaskDefinitionInfo
    {
        public string Family { get; set; } = string.Empty;
        public string? TaskDefinitionArn { get; set; }
        public int Revision { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string Cpu { get; set; } = "256";
        public string Memory { get; set; } = "512";
        public string NetworkMode { get; set; } = "awsvpc";
        public string? ExecutionRoleArn { get; set; }
        public string? TaskRoleArn { get; set; }
        public string RequiresCompatibilities { get; set; } = "FARGATE";
        public string? OsFamily { get; set; }
        public string? ContainerDefinitionsJson { get; set; }
        public int ContainerCount { get; set; }
    }

    /// <summary>Cloud-side ECS service returned by the provider during sync or creation.</summary>
    public class CloudEcsServiceInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceArn { get; set; }
        public string ClusterName { get; set; } = string.Empty;
        public string? ClusterArn { get; set; }
        public string? TaskDefinition { get; set; }
        public int DesiredCount { get; set; }
        public int RunningCount { get; set; }
        public int PendingCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LaunchType { get; set; } = "FARGATE";
        public string SchedulingStrategy { get; set; } = "REPLICA";
        public string? NetworkConfigurationJson { get; set; }
        public DateTime? ServiceCreatedAt { get; set; }
    }

    /// <summary>Cloud-side ECS task returned by the provider during sync or run.</summary>
    public class CloudEcsTaskInfo
    {
        public string TaskArn { get; set; } = string.Empty;
        public string ClusterName { get; set; } = string.Empty;
        public string? ClusterArn { get; set; }
        public string? TaskDefinitionArn { get; set; }
        public string? Group { get; set; }
        public string LastStatus { get; set; } = string.Empty;
        public string DesiredStatus { get; set; } = string.Empty;
        public string? Cpu { get; set; }
        public string? Memory { get; set; }
        public string LaunchType { get; set; } = "FARGATE";
        public string? Connectivity { get; set; }
        public string? StopCode { get; set; }
        public string? StoppedReason { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? PullStartedAt { get; set; }
        public DateTime? PullStoppedAt { get; set; }
    }
}
