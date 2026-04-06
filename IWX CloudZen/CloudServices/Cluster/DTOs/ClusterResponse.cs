namespace IWX_CloudZen.CloudServices.Cluster.DTOs
{
    public class ClusterResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ClusterArn { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public bool ContainerInsightsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
