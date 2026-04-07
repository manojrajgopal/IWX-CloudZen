namespace IWX_CloudZen.Permissions.DTOs
{
    public class PolicyAttachmentInfo
    {
        public string PolicyArn { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;

        /// <summary>"AWS Managed" | "Customer Managed" | "Inline"</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>"User" | "Group: {GroupName}"</summary>
        public string AttachedVia { get; set; } = string.Empty;
    }

    public class PermissionSummaryResponse
    {
        public string UserName { get; set; } = string.Empty;
        public string UserArn { get; set; } = string.Empty;
        public int AttachedManagedPoliciesCount { get; set; }
        public int InlinePoliciesCount { get; set; }
        public int GroupPoliciesCount { get; set; }
        public List<string> Groups { get; set; } = new();
        public List<PolicyAttachmentInfo> Policies { get; set; } = new();
    }
}
