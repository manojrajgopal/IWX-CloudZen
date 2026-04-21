namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// Response for dependency check: what resources block deletion of a target resource.
    /// </summary>
    public class DeletionBlockersResponse
    {
        /// <summary>The resource being checked for deletion.</summary>
        public ResourceSummary Resource { get; set; } = new();

        /// <summary>Whether this resource can be safely deleted (no blockers).</summary>
        public bool CanDelete { get; set; }

        /// <summary>Resources that must be deleted or detached first.</summary>
        public List<ResourceSummary> Blockers { get; set; } = new();

        /// <summary>Suggested deletion order (bottom-up). Delete in this sequence to avoid errors.</summary>
        public List<ResourceSummary> DeletionOrder { get; set; } = new();

        /// <summary>Human-readable messages explaining each blocker.</summary>
        public List<string> Messages { get; set; } = new();
    }
}
