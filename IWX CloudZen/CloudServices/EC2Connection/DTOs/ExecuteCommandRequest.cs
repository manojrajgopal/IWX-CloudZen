namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>Request to execute a command on an active SSH session.</summary>
    public record ExecuteCommandRequest(
        string SessionId,
        string Command
    );
}
