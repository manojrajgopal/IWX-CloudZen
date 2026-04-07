namespace IWX_CloudZen.Permissions.DTOs
{
    public class PolicyStatementResponse
    {
        public string Sid { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public List<string> NotActions { get; set; } = new();
        public List<string> Resources { get; set; } = new();
    }
}
