namespace IWX_CloudZen.CloudServices.EC2.DTOs
{
    public class SyncEc2InstancesResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<Ec2InstanceResponse> Instances { get; set; } = new();
    }
}
