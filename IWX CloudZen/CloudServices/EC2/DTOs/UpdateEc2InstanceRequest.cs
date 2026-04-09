namespace IWX_CloudZen.CloudServices.EC2.DTOs
{
    public record UpdateEc2InstanceRequest(
        string? InstanceName = null,
        string? InstanceType = null,
        List<string>? SecurityGroupIds = null,
        Dictionary<string, string>? Tags = null
    );
}
