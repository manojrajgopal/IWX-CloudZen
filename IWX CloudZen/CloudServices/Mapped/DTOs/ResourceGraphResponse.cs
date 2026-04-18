namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// Full dependency graph response for an account.
    /// </summary>
    public class ResourceGraphResponse
    {
        /// <summary>Tree-structured resource graph. Root nodes are top-level resources (VPCs, S3 Buckets, ECS Clusters, etc.).</summary>
        public List<ResourceNode> Graph { get; set; } = new();

        /// <summary>Flat list of all edges (relationships) in the graph.</summary>
        public List<ResourceEdge> Edges { get; set; } = new();

        /// <summary>Summary statistics per resource type.</summary>
        public Dictionary<string, int> Summary { get; set; } = new();

        /// <summary>Total number of resources.</summary>
        public int TotalResources { get; set; }
    }
}
