namespace IWX_CloudZen.CloudDeployments.Entities
{
    public class CloudDeployment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Provider {  get; set; }
        public int CloudAccountId { get; set; }
        public string DeploymentType { get; set; }
        public string? ImageUrl { get; set; }
        public string? ServiceName { get; set; }
        public string? ClusterName { get; set; }
        public string? HealthUrl { get; set; }
        public string? LogsGroup { get; set; }
        public string Status { get; set; }
        public string PackagePath { get; set; }
        public string UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdated {  get; set; }
    }
}
