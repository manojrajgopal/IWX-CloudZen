namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>Request to disconnect an active SSH session.</summary>
    public record DisconnectRequest(
        string SessionId
    );
}
