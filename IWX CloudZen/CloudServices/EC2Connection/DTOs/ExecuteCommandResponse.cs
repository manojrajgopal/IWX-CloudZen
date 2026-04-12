namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>Response containing the output of an executed command.</summary>
    public class ExecuteCommandResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
    }
}
