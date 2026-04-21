namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// Response for a specific resource's dependency info.
    /// </summary>
    public class ResourceDependencyResponse
    {
        /// <summary>The queried resource.</summary>
        public ResourceSummary Resource { get; set; } = new();

        /// <summary>Resources that this resource depends on (parents).</summary>
        public List<ResourceSummary> DependsOn { get; set; } = new();

        /// <summary>Resources that depend on this resource (children).</summary>
        public List<ResourceSummary> DependedBy { get; set; } = new();

        /// <summary>All edges involving this resource.</summary>
        public List<ResourceEdge> Edges { get; set; } = new();
    }
}
