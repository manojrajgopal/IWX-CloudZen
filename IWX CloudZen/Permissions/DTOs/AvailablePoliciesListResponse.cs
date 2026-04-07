namespace IWX_CloudZen.Permissions.DTOs
{
    public class AvailablePolicyResponse
    {
        public string PolicyArn { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>"AWS" | "Local"</summary>
        public string Scope { get; set; } = string.Empty;
    }

    public class AvailablePoliciesListResponse
    {
        public int TotalCount { get; set; }
        public List<AvailablePolicyResponse> Policies { get; set; } = new();
    }
}
