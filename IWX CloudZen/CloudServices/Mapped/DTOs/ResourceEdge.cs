namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// A single edge (relationship) between two resources.
    /// </summary>
    public class ResourceEdge
    {
        /// <summary>Source resource type + ID (parent).</summary>
        public string SourceType { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;

        /// <summary>Target resource type + ID (child/dependent).</summary>
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;

        /// <summary>Relationship type, e.g. "contains", "attached-to", "uses", "member-of".</summary>
        public string Relationship { get; set; } = string.Empty;
    }
}
