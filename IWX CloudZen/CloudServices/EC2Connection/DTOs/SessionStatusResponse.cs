namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>Status info for an active session.</summary>
    public class SessionStatusResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ConnectionMethod { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsUser { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public List<CommandLogEntry> CommandHistory { get; set; } = new();
    }

    /// <summary>A single command execution log entry.</summary>
    public class CommandLogEntry
    {
        public string Command { get; set; } = string.Empty;
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}
