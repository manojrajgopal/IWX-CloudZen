namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class Ec2InstanceConnectSessionListResponse
    {
        public List<Ec2InstanceConnectSessionResponse> Sessions { get; set; } = new();
    }
}
