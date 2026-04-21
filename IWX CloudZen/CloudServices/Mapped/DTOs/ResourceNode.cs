namespace IWX_CloudZen.CloudServices.Mapped.DTOs
{
    /// <summary>
    /// A single node in the resource dependency graph.
    /// Each node represents a cloud resource and its children (dependents).
    /// </summary>
    public class ResourceNode
    {
        /// <summary>Resource type, e.g. "VPC", "Subnet", "SecurityGroup", "EC2", "InternetGateway", etc.</summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>Cloud resource ID, e.g. "vpc-0abc123", "subnet-0abc123", "sg-0abc123".</summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>Human-readable name/label of this resource.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Current state/status (e.g. "available", "running", "active").</summary>
        public string State { get; set; } = string.Empty;

        /// <summary>Internal DB record ID.</summary>
        public int DbId { get; set; }

        /// <summary>Cloud provider: "AWS", "Azure", "GCP".</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Additional key-value metadata for this resource.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>Child resources that depend on or are contained within this resource.</summary>
        public List<ResourceNode> Children { get; set; } = new();

        /// <summary>Number of total descendants (recursive count).</summary>
        public int TotalDescendants { get; set; }
    }
}
