namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public class SyncEc2InstanceConnectEndpointsResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<Ec2InstanceConnectEndpointResponse> Endpoints { get; set; } = new();
    }
}
