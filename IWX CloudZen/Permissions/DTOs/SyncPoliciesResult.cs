namespace IWX_CloudZen.Permissions.DTOs
{
    public class SyncPoliciesResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<PolicyRecordResponse> Policies { get; set; } = new();
    }
}
