using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.Subnet.DTOs;
using IWX_CloudZen.CloudServices.Subnet.Interfaces;

using DtoCreateSubnet = IWX_CloudZen.CloudServices.Subnet.DTOs.CreateSubnetRequest;

namespace IWX_CloudZen.CloudServices.Subnet.Providers
{
    public class AwsSubnetProvider : ISubnetProvider
    {
        // ================================================================
        // Client
        // ================================================================

        private static AmazonEC2Client GetClient(CloudConnectionSecrets account) =>
            new AmazonEC2Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));

        // ================================================================
        // Helpers
        // ================================================================

        private static string GetNameTag(List<Tag>? tags) =>
            tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty;

        private static CloudSubnetInfo MapSubnet(Amazon.EC2.Model.Subnet s) => new()
        {
            SubnetId = s.SubnetId,
            Name = GetNameTag(s.Tags),
            VpcId = s.VpcId,
            CidrBlock = s.CidrBlock,
            Ipv6CidrBlock = s.Ipv6CidrBlockAssociationSet?
                .FirstOrDefault(a => a.Ipv6CidrBlockState?.State?.Value == "associated")
                ?.Ipv6CidrBlock,
            AvailabilityZone = s.AvailabilityZone,
            AvailabilityZoneId = s.AvailabilityZoneId,
            State = s.State?.Value ?? string.Empty,
            AvailableIpAddressCount = s.AvailableIpAddressCount.GetValueOrDefault(),
            IsDefault = s.DefaultForAz.GetValueOrDefault(),
            MapPublicIpOnLaunch = s.MapPublicIpOnLaunch.GetValueOrDefault(),
            AssignIpv6AddressOnCreation = s.AssignIpv6AddressOnCreation.GetValueOrDefault()
        };

        // ================================================================
        // Interface Implementation
        // ================================================================

        public async Task<List<CloudSubnetInfo>> FetchAllSubnets(
            CloudConnectionSecrets account, string? vpcId = null)
        {
            var client = GetClient(account);
            var result = new List<CloudSubnetInfo>();
            string? nextToken = null;

            var request = new DescribeSubnetsRequest();

            if (!string.IsNullOrWhiteSpace(vpcId))
            {
                request.Filters = new List<Filter>
                {
                    new Filter { Name = "vpc-id", Values = new List<string> { vpcId } }
                };
            }

            do
            {
                request.NextToken = nextToken;
                var response = await client.DescribeSubnetsAsync(request);

                result.AddRange(response.Subnets.Select(MapSubnet));
                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
        }

        public async Task<CloudSubnetInfo> CreateSubnet(
            CloudConnectionSecrets account, DtoCreateSubnet request)
        {
            var client = GetClient(account);

            var awsRequest = new Amazon.EC2.Model.CreateSubnetRequest
            {
                VpcId = request.VpcId,
                CidrBlock = request.CidrBlock,
                AvailabilityZone = request.AvailabilityZone
            };

            if (!string.IsNullOrWhiteSpace(request.Ipv6CidrBlock))
                awsRequest.Ipv6CidrBlock = request.Ipv6CidrBlock;

            var createResponse = await client.CreateSubnetAsync(awsRequest);
            var subnetId = createResponse.Subnet.SubnetId;

            // Tag with Name
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                await client.CreateTagsAsync(new CreateTagsRequest
                {
                    Resources = new List<string> { subnetId },
                    Tags = new List<Tag> { new Tag { Key = "Name", Value = request.Name } }
                });
            }

            // Set MapPublicIpOnLaunch if requested
            if (request.MapPublicIpOnLaunch)
            {
                await client.ModifySubnetAttributeAsync(new ModifySubnetAttributeRequest
                {
                    SubnetId = subnetId,
                    MapPublicIpOnLaunch = true
                });
            }

            // Describe to return the final state (tags + attributes resolved)
            var describeResponse = await client.DescribeSubnetsAsync(new DescribeSubnetsRequest
            {
                SubnetIds = new List<string> { subnetId }
            });

            var subnet = describeResponse.Subnets.FirstOrDefault()
                ?? throw new InvalidOperationException($"Subnet '{subnetId}' could not be described after creation.");

            return MapSubnet(subnet);
        }

        public async Task<CloudSubnetInfo> UpdateSubnet(
            CloudConnectionSecrets account, string subnetId, UpdateSubnetRequest request)
        {
            var client = GetClient(account);

            // Update Name tag
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                await client.CreateTagsAsync(new CreateTagsRequest
                {
                    Resources = new List<string> { subnetId },
                    Tags = new List<Tag> { new Tag { Key = "Name", Value = request.Name } }
                });
            }

            // Update MapPublicIpOnLaunch
            if (request.MapPublicIpOnLaunch.HasValue)
            {
                await client.ModifySubnetAttributeAsync(new ModifySubnetAttributeRequest
                {
                    SubnetId = subnetId,
                    MapPublicIpOnLaunch = request.MapPublicIpOnLaunch.Value
                });
            }

            // Update AssignIpv6AddressOnCreation
            if (request.AssignIpv6AddressOnCreation.HasValue)
            {
                await client.ModifySubnetAttributeAsync(new ModifySubnetAttributeRequest
                {
                    SubnetId = subnetId,
                    AssignIpv6AddressOnCreation = request.AssignIpv6AddressOnCreation.Value
                });
            }

            // Describe to return the updated state
            var describeResponse = await client.DescribeSubnetsAsync(new DescribeSubnetsRequest
            {
                SubnetIds = new List<string> { subnetId }
            });

            var subnet = describeResponse.Subnets.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Subnet '{subnetId}' not found in AWS.");

            return MapSubnet(subnet);
        }

        public async Task DeleteSubnet(CloudConnectionSecrets account, string subnetId)
        {
            var client = GetClient(account);

            await client.DeleteSubnetAsync(new DeleteSubnetRequest
            {
                SubnetId = subnetId
            });
        }
    }
}
