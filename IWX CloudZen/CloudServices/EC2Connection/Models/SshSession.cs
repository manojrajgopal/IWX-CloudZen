using Renci.SshNet;
using IWX_CloudZen.CloudServices.EC2Connection.DTOs;

namespace IWX_CloudZen.CloudServices.EC2Connection.Models
{
    /// <summary>
    /// Represents a live connection session held in memory.
    /// Supports both SSH (direct) and SSM (AWS Systems Manager) connection methods.
    /// Disposed when the session is disconnected.
    /// </summary>
    public sealed class SshSession : IDisposable
    {
        public string SessionId { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public string InstanceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsUser { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public List<CommandLogEntry> CommandHistory { get; set; } = new();

        /// <summary>Connection method: "SSH" or "SSM".</summary>
        public string ConnectionMethod { get; set; } = "SSM";

        /// <summary>The underlying SSH.NET client. Null for SSM sessions or after disposal.</summary>
        public SshClient? Client { get; set; }

        /// <summary>
        /// Interactive shell stream for maintaining state (pwd, env vars, etc.).
        /// Null for SSM sessions or if the session only uses RunCommand style execution.
        /// </summary>
        public ShellStream? ShellStream { get; set; }

        public bool IsConnected => ConnectionMethod == "SSM"
            ? true  // SSM sessions are always "connected" (stateless, API-based)
            : Client?.IsConnected == true;

        public void Dispose()
        {
            ShellStream?.Dispose();
            Client?.Dispose();
            ShellStream = null;
            Client = null;
        }
    }
}
