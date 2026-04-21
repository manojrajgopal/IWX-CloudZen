namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>
    /// Request to manually connect to any SSH host by providing
    /// connection details directly (no database instance record required).
    /// </summary>
    public record ManualConnectionRequest(
        /// <summary>Public or private IP address / hostname of the target instance.</summary>
        string IpAddress,

        /// <summary>OS user to connect as (e.g. "ec2-user", "ubuntu", "admin", "root").</summary>
        string OsUser,

        /// <summary>PEM private key content for SSH authentication.</summary>
        string PrivateKeyContent,

        /// <summary>Optional: a label for this connection (shown in the terminal banner). Defaults to the IP address.</summary>
        string? Label = null,

        /// <summary>SSH port (default 22).</summary>
        int Port = 22
    );
}
