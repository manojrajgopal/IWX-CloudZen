namespace IWX_CloudZen.CloudServices.EC2.DTOs
{
    public class CloudEc2InstanceInfo
    {
        public string InstanceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InstanceType { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PublicIpAddress { get; set; } = string.Empty;
        public string PrivateIpAddress { get; set; } = string.Empty;
        public string VpcId { get; set; } = string.Empty;
        public string SubnetId { get; set; } = string.Empty;
        public string ImageId { get; set; } = string.Empty;
        public string KeyName { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Monitoring { get; set; } = string.Empty;
        public bool EbsOptimized { get; set; }
        public List<Ec2SecurityGroupDto> SecurityGroups { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
        public DateTime? LaunchTime { get; set; }
    }

    public class Ec2SecurityGroupDto
    {
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
    }
}
