using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.EC2.DTOs;

namespace IWX_CloudZen.CloudServices.EC2.Interfaces
{
    public interface IEc2Provider
    {
        Task<List<CloudEc2InstanceInfo>> FetchAllInstances(CloudConnectionSecrets account);

        Task<CloudEc2InstanceInfo> GetInstance(CloudConnectionSecrets account, string instanceId);

        Task<List<CloudEc2InstanceInfo>> LaunchInstances(
            CloudConnectionSecrets account,
            string instanceName,
            string imageId,
            string instanceType,
            string? keyName,
            string? subnetId,
            List<string>? securityGroupIds,
            int minCount,
            int maxCount,
            bool ebsOptimized,
            string? userData,
            Dictionary<string, string>? tags);

        Task<CloudEc2InstanceInfo> UpdateInstance(
            CloudConnectionSecrets account,
            string instanceId,
            string? instanceName,
            string? instanceType,
            Dictionary<string, string>? tags);

        Task StartInstance(CloudConnectionSecrets account, string instanceId);

        Task StopInstance(CloudConnectionSecrets account, string instanceId, bool force = false);

        Task RebootInstance(CloudConnectionSecrets account, string instanceId);

        Task TerminateInstance(CloudConnectionSecrets account, string instanceId);
    }
}
