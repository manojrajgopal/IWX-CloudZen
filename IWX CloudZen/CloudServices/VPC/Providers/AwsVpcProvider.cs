using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.VPC.DTOs;
using IWX_CloudZen.CloudServices.VPC.Interfaces;

namespace IWX_CloudZen.CloudServices.VPC.Providers
{
    public class AwsVpcProvider : IVpcProvider
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

        private async Task<bool> GetVpcDnsAttribute(AmazonEC2Client client, string vpcId, string attribute)
        {
            var response = await client.DescribeVpcAttributeAsync(new DescribeVpcAttributeRequest
            {
                VpcId = vpcId,
                Attribute = attribute
            });
            return attribute == "enableDnsSupport"
                ? response.EnableDnsSupport == true
                : response.EnableDnsHostnames == true;
        }

        private static CloudVpcInfo MapVpc(Amazon.EC2.Model.Vpc vpc, bool dnsSupport, bool dnsHostnames) => new()
        {
            VpcId = vpc.VpcId,
            Name = GetNameTag(vpc.Tags),
            CidrBlock = vpc.CidrBlock,
            State = vpc.State.Value,
            IsDefault = vpc.IsDefault == true,
            EnableDnsSupport = dnsSupport,
            EnableDnsHostnames = dnsHostnames
        };

        // ---- Interface implementation ----

        public async Task<List<CloudVpcInfo>> FetchAllVpcs(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var result = new List<CloudVpcInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeVpcsAsync(new DescribeVpcsRequest
                {
                    NextToken = nextToken
                });

                foreach (var vpc in response.Vpcs)
                {
                    bool dnsSupport = await GetVpcDnsAttribute(client, vpc.VpcId, "enableDnsSupport");
                    bool dnsHostnames = await GetVpcDnsAttribute(client, vpc.VpcId, "enableDnsHostnames");
                    result.Add(MapVpc(vpc, dnsSupport, dnsHostnames));
                }

                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
        }

        public async Task<CloudVpcInfo> CreateVpc(
            CloudConnectionSecrets account,
            string vpcName,
            string cidrBlock,
            bool enableDnsSupport,
            bool enableDnsHostnames)
        {
            var client = GetClient(account);

            var createResponse = await client.CreateVpcAsync(new Amazon.EC2.Model.CreateVpcRequest
            {
                CidrBlock = cidrBlock
            });

            var vpcId = createResponse.Vpc.VpcId;

            // Tag with Name
            await client.CreateTagsAsync(new CreateTagsRequest
            {
                Resources = [vpcId],
                Tags = [new Tag { Key = "Name", Value = vpcName }]
            });

            // Set DNS attributes
            await client.ModifyVpcAttributeAsync(new ModifyVpcAttributeRequest
            {
                VpcId = vpcId,
                EnableDnsSupport = enableDnsSupport
            });

            await client.ModifyVpcAttributeAsync(new ModifyVpcAttributeRequest
            {
                VpcId = vpcId,
                EnableDnsHostnames = enableDnsHostnames
            });

            var vpc = createResponse.Vpc;
            return new CloudVpcInfo
            {
                VpcId = vpcId,
                Name = vpcName,
                CidrBlock = vpc.CidrBlock,
                State = vpc.State.Value,
                IsDefault = vpc.IsDefault == true,
                EnableDnsSupport = enableDnsSupport,
                EnableDnsHostnames = enableDnsHostnames
            };
        }

        public async Task<CloudVpcInfo> UpdateVpc(
            CloudConnectionSecrets account,
            string vpcId,
            string? vpcName,
            bool? enableDnsSupport,
            bool? enableDnsHostnames)
        {
            var client = GetClient(account);

            if (!string.IsNullOrWhiteSpace(vpcName))
            {
                await client.CreateTagsAsync(new CreateTagsRequest
                {
                    Resources = [vpcId],
                    Tags = [new Tag { Key = "Name", Value = vpcName }]
                });
            }

            if (enableDnsSupport.HasValue)
            {
                await client.ModifyVpcAttributeAsync(new ModifyVpcAttributeRequest
                {
                    VpcId = vpcId,
                    EnableDnsSupport = enableDnsSupport.Value
                });
            }

            if (enableDnsHostnames.HasValue)
            {
                await client.ModifyVpcAttributeAsync(new ModifyVpcAttributeRequest
                {
                    VpcId = vpcId,
                    EnableDnsHostnames = enableDnsHostnames.Value
                });
            }

            // Describe to return the current state
            var describeResponse = await client.DescribeVpcsAsync(new DescribeVpcsRequest
            {
                VpcIds = [vpcId]
            });

            var vpc = describeResponse.Vpcs.FirstOrDefault()
                ?? throw new KeyNotFoundException($"VPC '{vpcId}' not found in AWS.");

            bool dnsSupport = await GetVpcDnsAttribute(client, vpcId, "enableDnsSupport");
            bool dnsHostnames = await GetVpcDnsAttribute(client, vpcId, "enableDnsHostnames");

            return MapVpc(vpc, dnsSupport, dnsHostnames);
        }

        public async Task DeleteVpc(CloudConnectionSecrets account, string vpcId)
        {
            var client = GetClient(account);

            // 1. Disassociate Elastic IPs attached to network interfaces in this VPC
            var eniResponse = await client.DescribeNetworkInterfacesAsync(new DescribeNetworkInterfacesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });

            foreach (var eni in eniResponse.NetworkInterfaces ?? [])
            {
                var associationId = eni.Association?.AssociationId;
                if (!string.IsNullOrEmpty(associationId))
                {
                    await client.DisassociateAddressAsync(new DisassociateAddressRequest
                    {
                        AssociationId = associationId
                    });
                }
            }

            // 2. Detach and delete internet gateways
            var igwResponse = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                Filters = [new Filter { Name = "attachment.vpc-id", Values = [vpcId] }]
            });

            foreach (var igw in igwResponse.InternetGateways ?? [])
            {
                await client.DetachInternetGatewayAsync(new DetachInternetGatewayRequest
                {
                    InternetGatewayId = igw.InternetGatewayId,
                    VpcId = vpcId
                });
                await client.DeleteInternetGatewayAsync(new DeleteInternetGatewayRequest
                {
                    InternetGatewayId = igw.InternetGatewayId
                });
            }

            // 2. Delete subnets
            var subnetResponse = await client.DescribeSubnetsAsync(new DescribeSubnetsRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });

            foreach (var subnet in subnetResponse.Subnets ?? [])
            {
                await client.DeleteSubnetAsync(new DeleteSubnetRequest
                {
                    SubnetId = subnet.SubnetId
                });
            }

            // 3. Delete non-main route tables
            var rtResponse = await client.DescribeRouteTablesAsync(new DescribeRouteTablesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });

            foreach (var rt in rtResponse.RouteTables ?? [])
            {
                bool isMain = rt.Associations.Any(a => a.Main == true);
                if (isMain) continue;

                foreach (var assoc in rt.Associations.Where(a => !string.IsNullOrEmpty(a.RouteTableAssociationId)))
                {
                    await client.DisassociateRouteTableAsync(new DisassociateRouteTableRequest
                    {
                        AssociationId = assoc.RouteTableAssociationId
                    });
                }

                await client.DeleteRouteTableAsync(new DeleteRouteTableRequest
                {
                    RouteTableId = rt.RouteTableId
                });
            }

            // 4. Delete non-default security groups
            var sgResponse = await client.DescribeSecurityGroupsAsync(new DescribeSecurityGroupsRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });

            foreach (var sg in (sgResponse.SecurityGroups ?? []).Where(g => g.GroupName != "default"))
            {
                await client.DeleteSecurityGroupAsync(new DeleteSecurityGroupRequest
                {
                    GroupId = sg.GroupId
                });
            }

            // 5. Delete the VPC
            await client.DeleteVpcAsync(new DeleteVpcRequest { VpcId = vpcId });
        }
    }
}
