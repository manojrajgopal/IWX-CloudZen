using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.InternetGateway.DTOs;
using IWX_CloudZen.CloudServices.InternetGateway.Interfaces;

namespace IWX_CloudZen.CloudServices.InternetGateway.Providers
{
    public class AwsInternetGatewayProvider : IInternetGatewayProvider
    {
        private AmazonEC2Client GetClient(CloudConnectionSecrets account)
        {
            return new AmazonEC2Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region));
        }

        // ---- Helpers ----

        private static string GetNameTag(List<Tag>? tags)
            => tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty;

        private static CloudInternetGatewayInfo MapIgw(Amazon.EC2.Model.InternetGateway igw) => new()
        {
            InternetGatewayId = igw.InternetGatewayId,
            Name = GetNameTag(igw.Tags),
            AttachedVpcId = igw.Attachments?.FirstOrDefault(a => a.State?.Value == "available")?.VpcId,
            State = igw.Attachments?.FirstOrDefault()?.State?.Value ?? "detached",
            OwnerId = igw.OwnerId
        };

        // ---- Interface implementation ----

        public async Task<List<CloudInternetGatewayInfo>> FetchAllInternetGateways(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var result = new List<CloudInternetGatewayInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
                {
                    NextToken = nextToken
                });

                foreach (var igw in response.InternetGateways)
                {
                    result.Add(MapIgw(igw));
                }

                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
        }

        public async Task<CloudInternetGatewayInfo> CreateInternetGateway(
            CloudConnectionSecrets account,
            string name,
            string? vpcId)
        {
            var client = GetClient(account);

            var createResponse = await client.CreateInternetGatewayAsync(new Amazon.EC2.Model.CreateInternetGatewayRequest());

            var igwId = createResponse.InternetGateway.InternetGatewayId;

            // Tag with Name
            await client.CreateTagsAsync(new CreateTagsRequest
            {
                Resources = [igwId],
                Tags = [new Tag { Key = "Name", Value = name }]
            });

            // Optionally attach to VPC
            if (!string.IsNullOrWhiteSpace(vpcId))
            {
                await client.AttachInternetGatewayAsync(new Amazon.EC2.Model.AttachInternetGatewayRequest
                {
                    InternetGatewayId = igwId,
                    VpcId = vpcId
                });
            }

            // Describe to get the final state
            var describeResponse = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                InternetGatewayIds = [igwId]
            });

            var igw = describeResponse.InternetGateways.FirstOrDefault()
                ?? throw new InvalidOperationException($"Internet Gateway '{igwId}' was created but could not be described.");

            return MapIgw(igw);
        }

        public async Task<CloudInternetGatewayInfo> UpdateInternetGateway(
            CloudConnectionSecrets account,
            string internetGatewayId,
            string? name)
        {
            var client = GetClient(account);

            if (!string.IsNullOrWhiteSpace(name))
            {
                await client.CreateTagsAsync(new CreateTagsRequest
                {
                    Resources = [internetGatewayId],
                    Tags = [new Tag { Key = "Name", Value = name }]
                });
            }

            // Describe to return the current state
            var describeResponse = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                InternetGatewayIds = [internetGatewayId]
            });

            var igw = describeResponse.InternetGateways.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Internet Gateway '{internetGatewayId}' not found in AWS.");

            return MapIgw(igw);
        }

        public async Task DeleteInternetGateway(CloudConnectionSecrets account, string internetGatewayId)
        {
            var client = GetClient(account);

            // First describe to check for attachments
            var describeResponse = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                InternetGatewayIds = [internetGatewayId]
            });

            var igw = describeResponse.InternetGateways.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Internet Gateway '{internetGatewayId}' not found in AWS.");

            // Detach from all VPCs before deleting
            foreach (var attachment in igw.Attachments ?? [])
            {
                if (!string.IsNullOrEmpty(attachment.VpcId))
                {
                    await client.DetachInternetGatewayAsync(new Amazon.EC2.Model.DetachInternetGatewayRequest
                    {
                        InternetGatewayId = internetGatewayId,
                        VpcId = attachment.VpcId
                    });
                }
            }

            await client.DeleteInternetGatewayAsync(new DeleteInternetGatewayRequest
            {
                InternetGatewayId = internetGatewayId
            });
        }

        public async Task<CloudInternetGatewayInfo> AttachToVpc(
            CloudConnectionSecrets account,
            string internetGatewayId,
            string vpcId)
        {
            var client = GetClient(account);

            await client.AttachInternetGatewayAsync(new Amazon.EC2.Model.AttachInternetGatewayRequest
            {
                InternetGatewayId = internetGatewayId,
                VpcId = vpcId
            });

            // Describe to get updated state
            var describeResponse = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                InternetGatewayIds = [internetGatewayId]
            });

            var igw = describeResponse.InternetGateways.FirstOrDefault()
                ?? throw new KeyNotFoundException($"Internet Gateway '{internetGatewayId}' not found in AWS.");

            return MapIgw(igw);
        }

        public async Task DetachFromVpc(
            CloudConnectionSecrets account,
            string internetGatewayId,
            string vpcId)
        {
            var client = GetClient(account);

            await client.DetachInternetGatewayAsync(new Amazon.EC2.Model.DetachInternetGatewayRequest
            {
                InternetGatewayId = internetGatewayId,
                VpcId = vpcId
            });
        }

        public async Task<CloudInternetGatewayInfo?> GetInternetGatewayForVpc(
            CloudConnectionSecrets account,
            string vpcId)
        {
            var client = GetClient(account);

            var response = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                Filters =
                [
                    new Filter { Name = "attachment.vpc-id", Values = [vpcId] }
                ]
            });

            var igw = response.InternetGateways.FirstOrDefault();
            return igw is null ? null : MapIgw(igw);
        }
    }
}
