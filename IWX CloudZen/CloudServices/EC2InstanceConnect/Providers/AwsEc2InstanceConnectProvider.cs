using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2InstanceConnect;
using Amazon.EC2InstanceConnect.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Interfaces;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Providers
{
    public class AwsEc2InstanceConnectProvider : IEc2InstanceConnectProvider
    {
        private AmazonEC2Client GetEc2Client(CloudConnectionSecrets account)
        {
            return new AmazonEC2Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region));
        }

        private AmazonEC2InstanceConnectClient GetConnectClient(CloudConnectionSecrets account)
        {
            return new AmazonEC2InstanceConnectClient(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region));
        }

        // ---- Helpers ----

        private static CloudEc2InstanceConnectEndpointInfo MapEndpoint(Ec2InstanceConnectEndpoint endpoint) => new()
        {
            EndpointId = endpoint.InstanceConnectEndpointId ?? string.Empty,
            SubnetId = endpoint.SubnetId ?? string.Empty,
            VpcId = endpoint.VpcId ?? string.Empty,
            State = endpoint.State?.Value ?? string.Empty,
            DnsName = endpoint.DnsName ?? string.Empty,
            NetworkInterfaceId = endpoint.NetworkInterfaceIds?.FirstOrDefault() ?? string.Empty,
            AvailabilityZone = endpoint.AvailabilityZone ?? string.Empty,
            FipsDnsName = endpoint.FipsDnsName ?? string.Empty,
            PreserveClientIp = endpoint.PreserveClientIp ?? false,
            SecurityGroupIds = endpoint.SecurityGroupIds ?? new List<string>(),
            Tags = endpoint.Tags?
                .ToDictionary(t => t.Key, t => t.Value) ?? new()
        };

        // ---- EC2 Instance Connect Endpoints ----

        public async Task<List<CloudEc2InstanceConnectEndpointInfo>> FetchAllEndpoints(CloudConnectionSecrets account)
        {
            var client = GetEc2Client(account);
            var result = new List<CloudEc2InstanceConnectEndpointInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeInstanceConnectEndpointsAsync(
                    new DescribeInstanceConnectEndpointsRequest
                    {
                        NextToken = nextToken,
                        MaxResults = 50
                    });

                foreach (var endpoint in response.InstanceConnectEndpoints)
                {
                    // Skip endpoints in delete-complete state
                    if (endpoint.State?.Value == "delete-complete")
                        continue;

                    result.Add(MapEndpoint(endpoint));
                }

                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
        }

        public async Task<CloudEc2InstanceConnectEndpointInfo> GetEndpoint(CloudConnectionSecrets account, string endpointId)
        {
            var client = GetEc2Client(account);

            var response = await client.DescribeInstanceConnectEndpointsAsync(
                new DescribeInstanceConnectEndpointsRequest
                {
                    InstanceConnectEndpointIds = [endpointId]
                });

            var endpoint = response.InstanceConnectEndpoints.FirstOrDefault()
                ?? throw new KeyNotFoundException($"EC2 Instance Connect Endpoint '{endpointId}' not found.");

            return MapEndpoint(endpoint);
        }

        public async Task<CloudEc2InstanceConnectEndpointInfo> CreateEndpoint(
            CloudConnectionSecrets account,
            string subnetId,
            List<string>? securityGroupIds,
            bool preserveClientIp,
            Dictionary<string, string>? tags)
        {
            var client = GetEc2Client(account);

            var request = new CreateInstanceConnectEndpointRequest
            {
                SubnetId = subnetId,
                PreserveClientIp = preserveClientIp
            };

            if (securityGroupIds is { Count: > 0 })
                request.SecurityGroupIds = securityGroupIds;

            if (tags is { Count: > 0 })
            {
                request.TagSpecifications =
                [
                    new TagSpecification
                    {
                        ResourceType = ResourceType.InstanceConnectEndpoint,
                        Tags = tags.Select(kvp => new Tag { Key = kvp.Key, Value = kvp.Value }).ToList()
                    }
                ];
            }

            var response = await client.CreateInstanceConnectEndpointAsync(request);

            var created = response.InstanceConnectEndpoint
                ?? throw new InvalidOperationException("Failed to create EC2 Instance Connect Endpoint.");

            return MapEndpoint(created);
        }

        public async Task DeleteEndpoint(CloudConnectionSecrets account, string endpointId)
        {
            var client = GetEc2Client(account);

            await client.DeleteInstanceConnectEndpointAsync(
                new DeleteInstanceConnectEndpointRequest
                {
                    InstanceConnectEndpointId = endpointId
                });
        }

        // ---- Send SSH Public Key ----

        public async Task<SendSshPublicKeyResponse> SendSshPublicKey(
            CloudConnectionSecrets account,
            string instanceId,
            string instanceOsUser,
            string sshPublicKey,
            string? availabilityZone)
        {
            var client = GetConnectClient(account);

            var request = new Amazon.EC2InstanceConnect.Model.SendSSHPublicKeyRequest
            {
                InstanceId = instanceId,
                InstanceOSUser = instanceOsUser,
                SSHPublicKey = sshPublicKey
            };

            if (!string.IsNullOrWhiteSpace(availabilityZone))
                request.AvailabilityZone = availabilityZone;

            var response = await client.SendSSHPublicKeyAsync(request);

            return new SendSshPublicKeyResponse
            {
                Success = response.Success ?? false,
                RequestId = response.RequestId ?? string.Empty,
                InstanceId = instanceId,
                OsUser = instanceOsUser
            };
        }

        // ---- Send Serial Console SSH Public Key ----

        public async Task<SendSshPublicKeyResponse> SendSerialConsoleSshPublicKey(
            CloudConnectionSecrets account,
            string instanceId,
            string sshPublicKey,
            string? serialPort)
        {
            var client = GetConnectClient(account);

            var request = new Amazon.EC2InstanceConnect.Model.SendSerialConsoleSSHPublicKeyRequest
            {
                InstanceId = instanceId,
                SSHPublicKey = sshPublicKey
            };

            if (!string.IsNullOrWhiteSpace(serialPort))
                request.SerialPort = int.Parse(serialPort);

            var response = await client.SendSerialConsoleSSHPublicKeyAsync(request);

            return new SendSshPublicKeyResponse
            {
                Success = response.Success ?? false,
                RequestId = response.RequestId ?? string.Empty,
                InstanceId = instanceId,
                OsUser = "serial-console"
            };
        }
    }
}
