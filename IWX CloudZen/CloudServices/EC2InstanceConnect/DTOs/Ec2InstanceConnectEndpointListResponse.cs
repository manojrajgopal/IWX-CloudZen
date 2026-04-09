namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class Ec2InstanceConnectEndpointListResponse
    {
        public List<Ec2InstanceConnectEndpointResponse> Endpoints { get; set; } = new();
    }
}
