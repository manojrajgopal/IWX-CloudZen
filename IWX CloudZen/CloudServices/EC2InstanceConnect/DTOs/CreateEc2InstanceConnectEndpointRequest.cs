namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public record CreateEc2InstanceConnectEndpointRequest(
        string SubnetId,
        List<string>? SecurityGroupIds = null,
        bool PreserveClientIp = true,
        Dictionary<string, string>? Tags = null
    );
}
