using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Interfaces
{
    public interface IEc2InstanceConnectProvider
    {
        // ---- EC2 Instance Connect Endpoints ----

        Task<List<CloudEc2InstanceConnectEndpointInfo>> FetchAllEndpoints(CloudConnectionSecrets account);

        Task<CloudEc2InstanceConnectEndpointInfo> GetEndpoint(CloudConnectionSecrets account, string endpointId);

        Task<CloudEc2InstanceConnectEndpointInfo> CreateEndpoint(
            CloudConnectionSecrets account,
            string subnetId,
            List<string>? securityGroupIds,
            bool preserveClientIp,
            Dictionary<string, string>? tags);

        Task DeleteEndpoint(CloudConnectionSecrets account, string endpointId);

        // ---- Send SSH Public Key ----

        Task<SendSshPublicKeyResponse> SendSshPublicKey(
            CloudConnectionSecrets account,
            string instanceId,
            string instanceOsUser,
            string sshPublicKey,
            string? availabilityZone);

        // ---- Send Serial Console SSH Public Key ----

        Task<SendSshPublicKeyResponse> SendSerialConsoleSshPublicKey(
            CloudConnectionSecrets account,
            string instanceId,
            string sshPublicKey,
            string? serialPort);
    }
}
