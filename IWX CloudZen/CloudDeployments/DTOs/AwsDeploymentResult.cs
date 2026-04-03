namespace IWX_CloudZen.CloudDeployments.DTOs
{
    public class AwsDeploymentResult
    {
        public string Status { get; set; } = "Running";
        public string? ImageUrl { get; set; }
        public string? ServiceName { get; set; }
        public string? ClusterName { get; set; }
        public string? HealthUrl { get; set; }
        public string? LogsGroup { get; set; }
    }
}
