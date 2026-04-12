namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    /// <summary>Response returned when a connection session is established.</summary>
    public class StartConnectionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ConnectionMethod { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsUser { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }
}
