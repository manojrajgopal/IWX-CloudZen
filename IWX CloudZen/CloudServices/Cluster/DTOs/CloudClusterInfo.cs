namespace IWX_CloudZen.CloudServices.Cluster.DTOs
{
    public class CloudClusterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ClusterArn { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool ContainerInsightsEnabled { get; set; }
    }
}
