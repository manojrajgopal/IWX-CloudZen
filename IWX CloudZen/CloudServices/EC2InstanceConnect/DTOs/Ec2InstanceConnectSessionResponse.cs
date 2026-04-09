namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class Ec2InstanceConnectSessionResponse
    {
        public int Id { get; set; }
        public string InstanceId { get; set; } = string.Empty;
        public string InstanceOsUser { get; set; } = string.Empty;
        public string AvailabilityZone { get; set; } = string.Empty;
        public string SessionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
