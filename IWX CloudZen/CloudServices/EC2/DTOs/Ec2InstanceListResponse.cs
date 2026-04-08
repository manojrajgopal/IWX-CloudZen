namespace IWX_CloudZen.CloudServices.EC2.DTOs
{
    public class Ec2InstanceListResponse
    {
        public List<Ec2InstanceResponse> Instances { get; set; } = new();
    }
}
