namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class SendSshPublicKeyResponse
    {
        public bool Success { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public string OsUser { get; set; } = string.Empty;
    }
}
