namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>List of active sessions for the current user.</summary>
    public class ActiveSessionsListResponse
    {
        public List<ActiveSessionInfo> Sessions { get; set; } = new();
    }

    /// <summary>Summary info for an active session.</summary>
    public class ActiveSessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string ConnectionMethod { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsUser { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CommandCount { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
