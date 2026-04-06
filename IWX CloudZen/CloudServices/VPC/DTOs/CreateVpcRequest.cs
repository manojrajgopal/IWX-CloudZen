namespace IWX_CloudZen.CloudServices.VPC.DTOs
{
    public record CreateVpcRequest(
        string VpcName,
        string CidrBlock,
        bool EnableDnsSupport = true,
        bool EnableDnsHostnames = true
    );
}
