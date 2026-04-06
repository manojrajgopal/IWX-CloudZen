using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.VPC.DTOs;

namespace IWX_CloudZen.CloudServices.VPC.Interfaces
{
    public interface IVpcProvider
    {
        Task<List<CloudVpcInfo>> FetchAllVpcs(CloudConnectionSecrets account);

        Task<CloudVpcInfo> CreateVpc(CloudConnectionSecrets account, string vpcName, string cidrBlock, bool enableDnsSupport, bool enableDnsHostnames);

        Task<CloudVpcInfo> UpdateVpc(CloudConnectionSecrets account, string vpcId, string? vpcName, bool? enableDnsSupport, bool? enableDnsHostnames);

        Task DeleteVpc(CloudConnectionSecrets account, string vpcId);
    }
}
