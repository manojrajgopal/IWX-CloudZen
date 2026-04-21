using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.InternetGateway.DTOs;

namespace IWX_CloudZen.CloudServices.InternetGateway.Interfaces
{
    public interface IInternetGatewayProvider
    {
        Task<List<CloudInternetGatewayInfo>> FetchAllInternetGateways(CloudConnectionSecrets account);

        Task<CloudInternetGatewayInfo> CreateInternetGateway(CloudConnectionSecrets account, string name, string? vpcId);

        Task<CloudInternetGatewayInfo> UpdateInternetGateway(CloudConnectionSecrets account, string internetGatewayId, string? name);

        Task DeleteInternetGateway(CloudConnectionSecrets account, string internetGatewayId);

        Task<CloudInternetGatewayInfo> AttachToVpc(CloudConnectionSecrets account, string internetGatewayId, string vpcId);

        Task DetachFromVpc(CloudConnectionSecrets account, string internetGatewayId, string vpcId);

        Task<CloudInternetGatewayInfo?> GetInternetGatewayForVpc(CloudConnectionSecrets account, string vpcId);
    }
}
