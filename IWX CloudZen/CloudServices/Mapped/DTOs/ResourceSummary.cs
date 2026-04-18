namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// A flat resource entry used in summary lists.
    /// </summary>
    public class ResourceSummary
    {
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int DbId { get; set; }
        public string Provider { get; set; } = string.Empty;

        /// <summary>The parent resource ID this depends on (e.g. VpcId for a Subnet).</summary>
        public string? ParentResourceId { get; set; }

        /// <summary>The parent resource type.</summary>
        public string? ParentResourceType { get; set; }
    }
}
