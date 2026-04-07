namespace IWX_CloudZen.Permissions.DTOs
{
    public class PolicyListResponse
    {
        public string UserName { get; set; } = string.Empty;
        public string UserArn { get; set; } = string.Empty;
        public int TotalPolicies { get; set; }
        public List<PolicyResponse> Policies { get; set; } = new();
    }
}
