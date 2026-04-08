using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.Subnet.DTOs;

namespace IWX_CloudZen.CloudServices.Subnet.Interfaces
{
    public interface ISubnetProvider
    {
        /// <summary>Fetches all subnets in the account (optionally filtered by VPC ID).</summary>
        Task<List<CloudSubnetInfo>> FetchAllSubnets(CloudConnectionSecrets account, string? vpcId = null);

        /// <summary>Creates a new subnet inside a VPC.</summary>
        Task<CloudSubnetInfo> CreateSubnet(CloudConnectionSecrets account, CreateSubnetRequest request);

        /// <summary>Updates mutable attributes of an existing subnet.</summary>
        Task<CloudSubnetInfo> UpdateSubnet(CloudConnectionSecrets account, string subnetId, UpdateSubnetRequest request);

        /// <summary>Deletes a subnet from the cloud.</summary>
        Task DeleteSubnet(CloudConnectionSecrets account, string subnetId);
    }
}
