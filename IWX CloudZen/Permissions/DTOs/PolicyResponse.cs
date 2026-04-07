namespace IWX_CloudZen.Permissions.DTOs
{
    public class PolicyResponse
    {
        public string PolicyArn { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;

        /// <summary>"AWS Managed" | "Customer Managed" | "Inline"</summary>
        public string PolicyType { get; set; } = string.Empty;

        /// <summary>"User" | "Group: {GroupName}"</summary>
        public string AttachedVia { get; set; } = string.Empty;

        public List<PolicyStatementResponse> Statements { get; set; } = new();
    }
}
