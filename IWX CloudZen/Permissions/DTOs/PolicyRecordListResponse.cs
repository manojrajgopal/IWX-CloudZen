namespace IWX_CloudZen.Permissions.DTOs
{
    public class PolicyRecordListResponse
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalPolicies { get; set; }
        public List<PolicyRecordResponse> Policies { get; set; } = new();
    }
}
