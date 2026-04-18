using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.Mapped.DTOs;

namespace IWX_CloudZen.CloudServices.Mapped.Interfaces
{
    /// <summary>
    /// Provider interface for fetching live resource dependency data from a cloud platform.
    /// Implementations query the cloud APIs to discover real-time relationships.
    /// </summary>
    public interface IMappedProvider
    {
        /// <summary>
        /// Fetch all resources and their relationships for the given account.
        /// Returns a flat list of edges discovered from the cloud.
        /// </summary>
        Task<List<ResourceEdge>> FetchAllResourceEdges(CloudConnectionSecrets account);

        /// <summary>
        /// Fetch all resources inside a specific VPC from the cloud.
        /// </summary>
        Task<List<ResourceSummary>> FetchVpcResources(CloudConnectionSecrets account, string vpcId);

        /// <summary>
        /// Check what Elastic IPs, NAT Gateways, or other mapped public addresses
        /// are associated with a VPC's network interfaces. This is the live check
        /// that explains "mapped public address" errors.
        /// </summary>
        Task<List<ResourceSummary>> FetchMappedPublicAddresses(CloudConnectionSecrets account, string vpcId);

        /// <summary>
        /// Fetch all network interfaces in a VPC to discover hidden dependencies.
        /// </summary>
        Task<List<ResourceSummary>> FetchNetworkInterfaces(CloudConnectionSecrets account, string vpcId);
    }
}
