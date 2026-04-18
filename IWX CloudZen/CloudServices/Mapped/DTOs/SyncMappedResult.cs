namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// Result of syncing (refreshing) the resource graph from cloud.
    /// </summary>
    public class SyncMappedResult
    {
        public int TotalResources { get; set; }
        public Dictionary<string, int> ResourceCounts { get; set; } = new();
        public int TotalEdges { get; set; }
        public ResourceGraphResponse Graph { get; set; } = new();
    }
}
