namespace IWX_CloudZen.CloudServices.Cluster.DTOs
{
    public class SyncResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<ClusterResponse> Clusters { get; set; } = new();
    }
}
