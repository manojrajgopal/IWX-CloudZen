namespace IWX_CloudZen.CloudServices.EC2.DTOs
{
    public record LaunchEc2InstanceRequest(
        string InstanceName,
        string ImageId,
        string InstanceType,
        string? KeyName = null,
        string? SubnetId = null,
        List<string>? SecurityGroupIds = null,
        int MinCount = 1,
        int MaxCount = 1,
        bool EbsOptimized = false,
        string? UserData = null,
        Dictionary<string, string>? Tags = null
    );
}
