using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.Mapped.DTOs;
using IWX_CloudZen.CloudServices.Mapped.Interfaces;

namespace IWX_CloudZen.CloudServices.Mapped.Providers
{
    public class AwsMappedProvider : IMappedProvider
    {
        private AmazonEC2Client GetClient(CloudConnectionSecrets account)
        {
            return new AmazonEC2Client(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region));
        }

        private static string GetNameTag(List<Tag>? tags)
            => tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty;

        // ---- Fetch all resource edges from cloud ----

        public async Task<List<ResourceEdge>> FetchAllResourceEdges(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var edges = new List<ResourceEdge>();

            // 1. VPCs
            var vpcs = await client.DescribeVpcsAsync(new DescribeVpcsRequest());

            foreach (var vpc in vpcs.Vpcs ?? [])
            {
                var vpcName = GetNameTag(vpc.Tags);

                // 2. Subnets in VPC
                var subnets = await client.DescribeSubnetsAsync(new DescribeSubnetsRequest
                {
                    Filters = [new Filter { Name = "vpc-id", Values = [vpc.VpcId] }]
                });

                foreach (var subnet in subnets.Subnets ?? [])
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = vpc.VpcId, SourceName = vpcName,
                        TargetType = "Subnet", TargetId = subnet.SubnetId, TargetName = GetNameTag(subnet.Tags),
                        Relationship = "contains"
                    });
                }

                // 3. Security Groups in VPC
                var sgs = await client.DescribeSecurityGroupsAsync(new DescribeSecurityGroupsRequest
                {
                    Filters = [new Filter { Name = "vpc-id", Values = [vpc.VpcId] }]
                });

                foreach (var sg in sgs.SecurityGroups ?? [])
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = vpc.VpcId, SourceName = vpcName,
                        TargetType = "SecurityGroup", TargetId = sg.GroupId, TargetName = sg.GroupName,
                        Relationship = "contains"
                    });
                }

                // 4. Internet Gateways attached to VPC
                var igws = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
                {
                    Filters = [new Filter { Name = "attachment.vpc-id", Values = [vpc.VpcId] }]
                });

                foreach (var igw in igws.InternetGateways ?? [])
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = vpc.VpcId, SourceName = vpcName,
                        TargetType = "InternetGateway", TargetId = igw.InternetGatewayId, TargetName = GetNameTag(igw.Tags),
                        Relationship = "attached-to"
                    });
                }

                // 5. NAT Gateways in VPC
                var natGws = await client.DescribeNatGatewaysAsync(new DescribeNatGatewaysRequest
                {
                    Filter = [new Filter { Name = "vpc-id", Values = [vpc.VpcId] }]
                });

                foreach (var nat in natGws.NatGateways ?? [])
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = vpc.VpcId, SourceName = vpcName,
                        TargetType = "NATGateway", TargetId = nat.NatGatewayId, TargetName = GetNameTag(nat.Tags),
                        Relationship = "contains"
                    });

                    // NAT Gateway → Subnet
                    if (!string.IsNullOrEmpty(nat.SubnetId))
                    {
                        edges.Add(new ResourceEdge
                        {
                            SourceType = "Subnet", SourceId = nat.SubnetId, SourceName = "",
                            TargetType = "NATGateway", TargetId = nat.NatGatewayId, TargetName = GetNameTag(nat.Tags),
                            Relationship = "contains"
                        });
                    }

                    // NAT Gateway → Elastic IP
                    foreach (var addr in nat.NatGatewayAddresses ?? [])
                    {
                        if (!string.IsNullOrEmpty(addr.AllocationId))
                        {
                            edges.Add(new ResourceEdge
                            {
                                SourceType = "NATGateway", SourceId = nat.NatGatewayId, SourceName = GetNameTag(nat.Tags),
                                TargetType = "ElasticIP", TargetId = addr.AllocationId, TargetName = addr.PublicIp ?? "",
                                Relationship = "uses"
                            });
                        }
                    }
                }

                // 6. Route Tables in VPC
                var rts = await client.DescribeRouteTablesAsync(new DescribeRouteTablesRequest
                {
                    Filters = [new Filter { Name = "vpc-id", Values = [vpc.VpcId] }]
                });

                foreach (var rt in rts.RouteTables ?? [])
                {
                    var isMain = (rt.Associations ?? []).Any(a => a.Main == true);
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = vpc.VpcId, SourceName = vpcName,
                        TargetType = "RouteTable", TargetId = rt.RouteTableId, TargetName = GetNameTag(rt.Tags),
                        Relationship = isMain ? "main-route-table" : "contains"
                    });

                    // Route Table → Subnet associations
                    foreach (var assoc in (rt.Associations ?? []).Where(a => !string.IsNullOrEmpty(a.SubnetId)))
                    {
                        edges.Add(new ResourceEdge
                        {
                            SourceType = "RouteTable", SourceId = rt.RouteTableId, SourceName = GetNameTag(rt.Tags),
                            TargetType = "Subnet", TargetId = assoc.SubnetId, TargetName = "",
                            Relationship = "associated-with"
                        });
                    }

                    // Route Table → IGW routes
                    foreach (var route in (rt.Routes ?? []).Where(r => !string.IsNullOrEmpty(r.GatewayId) && r.GatewayId != "local"))
                    {
                        edges.Add(new ResourceEdge
                        {
                            SourceType = "RouteTable", SourceId = rt.RouteTableId, SourceName = GetNameTag(rt.Tags),
                            TargetType = "InternetGateway", TargetId = route.GatewayId, TargetName = "",
                            Relationship = "routes-to"
                        });
                    }

                    // Route Table → NAT Gateway routes
                    foreach (var route in (rt.Routes ?? []).Where(r => !string.IsNullOrEmpty(r.NatGatewayId)))
                    {
                        edges.Add(new ResourceEdge
                        {
                            SourceType = "RouteTable", SourceId = rt.RouteTableId, SourceName = GetNameTag(rt.Tags),
                            TargetType = "NATGateway", TargetId = route.NatGatewayId, TargetName = "",
                            Relationship = "routes-to"
                        });
                    }
                }
            }

            // 7. EC2 Instances → VPC, Subnet, SecurityGroups, KeyPair
            var instances = await DescribeAllInstances(client);

            foreach (var instance in instances)
            {
                var instName = GetNameTag(instance.Tags);

                if (!string.IsNullOrEmpty(instance.VpcId))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = instance.VpcId, SourceName = "",
                        TargetType = "EC2", TargetId = instance.InstanceId, TargetName = instName,
                        Relationship = "contains"
                    });
                }

                if (!string.IsNullOrEmpty(instance.SubnetId))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "Subnet", SourceId = instance.SubnetId, SourceName = "",
                        TargetType = "EC2", TargetId = instance.InstanceId, TargetName = instName,
                        Relationship = "contains"
                    });
                }

                foreach (var sg in instance.SecurityGroups ?? [])
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "SecurityGroup", SourceId = sg.GroupId, SourceName = sg.GroupName,
                        TargetType = "EC2", TargetId = instance.InstanceId, TargetName = instName,
                        Relationship = "protects"
                    });
                }

                if (!string.IsNullOrEmpty(instance.KeyName))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "KeyPair", SourceId = instance.KeyName, SourceName = instance.KeyName,
                        TargetType = "EC2", TargetId = instance.InstanceId, TargetName = instName,
                        Relationship = "used-by"
                    });
                }

                // EC2 → Elastic IP
                if (!string.IsNullOrEmpty(instance.PublicIpAddress))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "EC2", SourceId = instance.InstanceId, SourceName = instName,
                        TargetType = "ElasticIP", TargetId = instance.PublicIpAddress, TargetName = instance.PublicIpAddress,
                        Relationship = "has-public-ip"
                    });
                }
            }

            // 8. Network Interfaces (ENIs) — these cause the "mapped public address" errors
            var enis = await DescribeAllNetworkInterfaces(client);

            foreach (var eni in enis)
            {
                if (!string.IsNullOrEmpty(eni.VpcId))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "VPC", SourceId = eni.VpcId, SourceName = "",
                        TargetType = "NetworkInterface", TargetId = eni.NetworkInterfaceId, TargetName = eni.Description ?? "",
                        Relationship = "contains"
                    });
                }

                if (!string.IsNullOrEmpty(eni.SubnetId))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "Subnet", SourceId = eni.SubnetId, SourceName = "",
                        TargetType = "NetworkInterface", TargetId = eni.NetworkInterfaceId, TargetName = eni.Description ?? "",
                        Relationship = "contains"
                    });
                }

                // ENI → Elastic IP association (mapped public address)
                if (eni.Association != null && !string.IsNullOrEmpty(eni.Association.PublicIp))
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "NetworkInterface", SourceId = eni.NetworkInterfaceId, SourceName = eni.Description ?? "",
                        TargetType = "ElasticIP", TargetId = eni.Association.AllocationId ?? eni.Association.PublicIp,
                        TargetName = eni.Association.PublicIp,
                        Relationship = "mapped-public-address"
                    });
                }

                foreach (var sg in eni.Groups ?? [])
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = "SecurityGroup", SourceId = sg.GroupId, SourceName = sg.GroupName,
                        TargetType = "NetworkInterface", TargetId = eni.NetworkInterfaceId, TargetName = eni.Description ?? "",
                        Relationship = "protects"
                    });
                }
            }

            return edges;
        }

        // ---- Fetch all resources inside a specific VPC ----

        public async Task<List<ResourceSummary>> FetchVpcResources(CloudConnectionSecrets account, string vpcId)
        {
            var client = GetClient(account);
            var resources = new List<ResourceSummary>();

            // Subnets
            var subnets = await client.DescribeSubnetsAsync(new DescribeSubnetsRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });
            foreach (var s in subnets.Subnets ?? [])
            {
                resources.Add(new ResourceSummary
                {
                    ResourceType = "Subnet", ResourceId = s.SubnetId, Name = GetNameTag(s.Tags),
                    State = s.State?.Value ?? "", Provider = "AWS",
                    ParentResourceId = vpcId, ParentResourceType = "VPC"
                });
            }

            // Security Groups
            var sgs = await client.DescribeSecurityGroupsAsync(new DescribeSecurityGroupsRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });
            foreach (var sg in sgs.SecurityGroups ?? [])
            {
                resources.Add(new ResourceSummary
                {
                    ResourceType = "SecurityGroup", ResourceId = sg.GroupId, Name = sg.GroupName,
                    State = "active", Provider = "AWS",
                    ParentResourceId = vpcId, ParentResourceType = "VPC"
                });
            }

            // Internet Gateways
            var igws = await client.DescribeInternetGatewaysAsync(new DescribeInternetGatewaysRequest
            {
                Filters = [new Filter { Name = "attachment.vpc-id", Values = [vpcId] }]
            });
            foreach (var igw in igws.InternetGateways ?? [])
            {
                resources.Add(new ResourceSummary
                {
                    ResourceType = "InternetGateway", ResourceId = igw.InternetGatewayId, Name = GetNameTag(igw.Tags),
                    State = igw.Attachments?.FirstOrDefault()?.State?.Value ?? "detached", Provider = "AWS",
                    ParentResourceId = vpcId, ParentResourceType = "VPC"
                });
            }

            // NAT Gateways
            var natGws = await client.DescribeNatGatewaysAsync(new DescribeNatGatewaysRequest
            {
                Filter = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });
            foreach (var nat in natGws.NatGateways ?? [])
            {
                resources.Add(new ResourceSummary
                {
                    ResourceType = "NATGateway", ResourceId = nat.NatGatewayId, Name = GetNameTag(nat.Tags),
                    State = nat.State?.Value ?? "", Provider = "AWS",
                    ParentResourceId = vpcId, ParentResourceType = "VPC"
                });
            }

            // Route Tables
            var rts = await client.DescribeRouteTablesAsync(new DescribeRouteTablesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });
            foreach (var rt in rts.RouteTables ?? [])
            {
                var isMain = (rt.Associations ?? []).Any(a => a.Main == true);
                resources.Add(new ResourceSummary
                {
                    ResourceType = "RouteTable", ResourceId = rt.RouteTableId,
                    Name = isMain ? "(Main) " + GetNameTag(rt.Tags) : GetNameTag(rt.Tags),
                    State = "active", Provider = "AWS",
                    ParentResourceId = vpcId, ParentResourceType = "VPC"
                });
            }

            // EC2 Instances in VPC
            var ec2s = await client.DescribeInstancesAsync(new DescribeInstancesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });
            foreach (var reservation in ec2s.Reservations ?? [])
            {
                foreach (var inst in reservation.Instances ?? [])
                {
                    resources.Add(new ResourceSummary
                    {
                        ResourceType = "EC2", ResourceId = inst.InstanceId, Name = GetNameTag(inst.Tags),
                        State = inst.State?.Name?.Value ?? "", Provider = "AWS",
                        ParentResourceId = inst.SubnetId, ParentResourceType = "Subnet"
                    });
                }
            }

            // Network Interfaces in VPC
            var enis = await client.DescribeNetworkInterfacesAsync(new DescribeNetworkInterfacesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });
            foreach (var eni in enis.NetworkInterfaces ?? [])
            {
                resources.Add(new ResourceSummary
                {
                    ResourceType = "NetworkInterface", ResourceId = eni.NetworkInterfaceId,
                    Name = eni.Description ?? eni.NetworkInterfaceId,
                    State = eni.Status?.Value ?? "", Provider = "AWS",
                    ParentResourceId = eni.SubnetId, ParentResourceType = "Subnet"
                });
            }

            return resources;
        }

        // ---- Fetch mapped public addresses (the cause of IGW detach errors) ----

        public async Task<List<ResourceSummary>> FetchMappedPublicAddresses(CloudConnectionSecrets account, string vpcId)
        {
            var client = GetClient(account);
            var mapped = new List<ResourceSummary>();

            // Find all ENIs in this VPC with public IP associations
            var enis = await client.DescribeNetworkInterfacesAsync(new DescribeNetworkInterfacesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });

            foreach (var eni in enis.NetworkInterfaces ?? [])
            {
                if (eni.Association != null && !string.IsNullOrEmpty(eni.Association.PublicIp))
                {
                    mapped.Add(new ResourceSummary
                    {
                        ResourceType = "ElasticIP",
                        ResourceId = eni.Association.AllocationId ?? eni.Association.PublicIp,
                        Name = $"{eni.Association.PublicIp} (on {eni.NetworkInterfaceId})",
                        State = "associated",
                        Provider = "AWS",
                        ParentResourceId = eni.NetworkInterfaceId,
                        ParentResourceType = "NetworkInterface"
                    });
                }
            }

            // Also find standalone Elastic IPs associated with this VPC's instances
            var addresses = await client.DescribeAddressesAsync(new DescribeAddressesRequest());
            foreach (var addr in addresses.Addresses ?? [])
            {
                if (!string.IsNullOrEmpty(addr.NetworkInterfaceId))
                {
                    var matchedEni = (enis.NetworkInterfaces ?? []).FirstOrDefault(e => e.NetworkInterfaceId == addr.NetworkInterfaceId);
                    if (matchedEni != null)
                    {
                        // Avoid duplicates
                        if (!mapped.Any(m => m.ResourceId == addr.AllocationId))
                        {
                            mapped.Add(new ResourceSummary
                            {
                                ResourceType = "ElasticIP",
                                ResourceId = addr.AllocationId,
                                Name = $"{addr.PublicIp} (Allocation: {addr.AllocationId})",
                                State = string.IsNullOrEmpty(addr.AssociationId) ? "available" : "associated",
                                Provider = "AWS",
                                ParentResourceId = addr.NetworkInterfaceId,
                                ParentResourceType = "NetworkInterface"
                            });
                        }
                    }
                }
            }

            return mapped;
        }

        // ---- Fetch network interfaces in a VPC ----

        public async Task<List<ResourceSummary>> FetchNetworkInterfaces(CloudConnectionSecrets account, string vpcId)
        {
            var client = GetClient(account);
            var result = new List<ResourceSummary>();

            var enis = await client.DescribeNetworkInterfacesAsync(new DescribeNetworkInterfacesRequest
            {
                Filters = [new Filter { Name = "vpc-id", Values = [vpcId] }]
            });

            foreach (var eni in enis.NetworkInterfaces ?? [])
            {
                var hasPublicIp = eni.Association != null && !string.IsNullOrEmpty(eni.Association.PublicIp);
                result.Add(new ResourceSummary
                {
                    ResourceType = "NetworkInterface",
                    ResourceId = eni.NetworkInterfaceId,
                    Name = $"{eni.Description ?? eni.NetworkInterfaceId}{(hasPublicIp ? $" [Public: {eni.Association!.PublicIp}]" : "")}",
                    State = eni.Status?.Value ?? "",
                    Provider = "AWS",
                    ParentResourceId = eni.SubnetId,
                    ParentResourceType = "Subnet"
                });
            }

            return result;
        }

        // ---- Helpers ----

        private static async Task<List<Instance>> DescribeAllInstances(AmazonEC2Client client)
        {
            var instances = new List<Instance>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeInstancesAsync(new DescribeInstancesRequest
                {
                    NextToken = nextToken
                });

                foreach (var reservation in response.Reservations)
                {
                    instances.AddRange(reservation.Instances);
                }

                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return instances;
        }

        private static async Task<List<NetworkInterface>> DescribeAllNetworkInterfaces(AmazonEC2Client client)
        {
            var enis = new List<NetworkInterface>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeNetworkInterfacesAsync(new DescribeNetworkInterfacesRequest
                {
                    NextToken = nextToken
                });

                enis.AddRange(response.NetworkInterfaces);
                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return enis;
        }
    }
}
