namespace IWX_CloudZen.CloudServices.ECS.DTOs
{
    // ================================================================
    // Task Definition Responses
    // ================================================================

    public class TaskDefinitionResponse
    {
        public int Id { get; set; }
        public string Family { get; set; } = string.Empty;
        public string? TaskDefinitionArn { get; set; }
        public int Revision { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Cpu { get; set; } = string.Empty;
        public string Memory { get; set; } = string.Empty;
        public string NetworkMode { get; set; } = string.Empty;
        public string? ExecutionRoleArn { get; set; }
        public string? TaskRoleArn { get; set; }
        public string RequiresCompatibilities { get; set; } = string.Empty;
        public string? OsFamily { get; set; }
        public int ContainerCount { get; set; }
        public string? ContainerDefinitionsJson { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TaskDefinitionListResponse
    {
        public int TotalCount { get; set; }
        public string? FamilyFilter { get; set; }
        public List<TaskDefinitionResponse> TaskDefinitions { get; set; } = new();
    }

    // ================================================================
    // ECS Service Responses
    // ================================================================

    public class EcsServiceResponse
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceArn { get; set; }
        public string ClusterName { get; set; } = string.Empty;
        public string? ClusterArn { get; set; }
        public string? TaskDefinition { get; set; }
        public int DesiredCount { get; set; }
        public int RunningCount { get; set; }
        public int PendingCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LaunchType { get; set; } = string.Empty;
        public string SchedulingStrategy { get; set; } = string.Empty;
        public string? NetworkConfigurationJson { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime? ServiceCreatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class EcsServiceListResponse
    {
        public string ClusterName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public List<EcsServiceResponse> Services { get; set; } = new();
    }

    // ================================================================
    // ECS Task Responses
    // ================================================================

    public class EcsTaskResponse
    {
        public int Id { get; set; }
        public string TaskArn { get; set; } = string.Empty;
        public string ClusterName { get; set; } = string.Empty;
        public string? ClusterArn { get; set; }
        public string? TaskDefinitionArn { get; set; }
        public string? Group { get; set; }
        public string LastStatus { get; set; } = string.Empty;
        public string DesiredStatus { get; set; } = string.Empty;
        public string? Cpu { get; set; }
        public string? Memory { get; set; }
        public string LaunchType { get; set; } = string.Empty;
        public string? Connectivity { get; set; }
        public string? StopCode { get; set; }
        public string? StoppedReason { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public DateTime? PullStartedAt { get; set; }
        public DateTime? PullStoppedAt { get; set; }
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class EcsTaskListResponse
    {
        public string ClusterName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public List<EcsTaskResponse> Tasks { get; set; } = new();
    }

    // ================================================================
    // Sync Results
    // ================================================================

    public class SyncTaskDefinitionsResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<TaskDefinitionResponse> TaskDefinitions { get; set; } = new();
    }

    public class SyncEcsServicesResult
    {
        public string ClusterName { get; set; } = string.Empty;
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<EcsServiceResponse> Services { get; set; } = new();
    }

    public class SyncEcsTasksResult
    {
        public string ClusterName { get; set; } = string.Empty;
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<EcsTaskResponse> Tasks { get; set; } = new();
    }

    public class FullEcsSyncResult
    {
        public SyncTaskDefinitionsResult TaskDefinitions { get; set; } = new();
        public List<SyncEcsServicesResult> ServicesByCluster { get; set; } = new();
        public List<SyncEcsTasksResult> TasksByCluster { get; set; } = new();
        public List<string> ClustersSynced { get; set; } = new();
        public DateTime SyncedAt { get; set; }
    }
}
