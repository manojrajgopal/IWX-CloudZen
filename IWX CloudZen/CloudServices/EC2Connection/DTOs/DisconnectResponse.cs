namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>Response when a session is disconnected.</summary>
    public class DisconnectResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DisconnectedAt { get; set; }
    }
}
