namespace IWX_CloudZen.CloudServices.VPC.DTOs
{
    public record UpdateVpcRequest(
        string? VpcName,
        bool? EnableDnsSupport,
        bool? EnableDnsHostnames
    );
}
