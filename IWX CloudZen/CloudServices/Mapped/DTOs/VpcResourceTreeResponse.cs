namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// Response for a single VPC's full resource tree.
    /// </summary>
    public class VpcResourceTreeResponse
    {
        public ResourceNode VpcTree { get; set; } = new();

        /// <summary>Flat list of all resources inside this VPC.</summary>
        public List<ResourceSummary> AllResources { get; set; } = new();

        /// <summary>All edges within this VPC.</summary>
        public List<ResourceEdge> Edges { get; set; } = new();

        /// <summary>Count per resource type inside this VPC.</summary>
        public Dictionary<string, int> Summary { get; set; } = new();

        /// <summary>Whether this VPC has an Internet Gateway.</summary>
        public bool HasInternetGateway { get; set; }

        /// <summary>Total number of resources inside this VPC.</summary>
        public int TotalResources { get; set; }
    }
}
