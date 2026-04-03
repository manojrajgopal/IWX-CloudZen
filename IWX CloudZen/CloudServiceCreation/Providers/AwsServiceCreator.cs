using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServiceCreation.DTOs;
using IWX_CloudZen.CloudServiceCreation.Interfaces;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using Ec2Tag = Amazon.EC2.Model.Tag;

namespace IWX_CloudZen.CloudServiceCreation.Providers
{
    public class AwsServiceCreator : ICloudServiceCreator
    {
        private AmazonEC2Client _ec2 = null!;
        private AmazonECSClient _ecs = null!;
        private AmazonElasticLoadBalancingV2Client _elb = null!;
        private AmazonIdentityManagementServiceClient _iam = null!;
        private AmazonCloudWatchLogsClient _logs = null!;
        private AmazonSecurityTokenServiceClient _sts = null!;
        private string _region = string.Empty;

        private void Init(CloudConnectionSecrets account)
        {
            _region = account.Region ?? throw new Exception("AWS region is required.");

            var endpoint = RegionEndpoint.GetBySystemName(_region);

            _ec2 = new AmazonEC2Client(account.AccessKey, account.SecretKey, endpoint);
            _ecs = new AmazonECSClient(account.AccessKey, account.SecretKey, endpoint);
            _elb = new AmazonElasticLoadBalancingV2Client(account.AccessKey, account.SecretKey, endpoint);
            _iam = new AmazonIdentityManagementServiceClient(account.AccessKey, account.SecretKey, endpoint);
            _logs = new AmazonCloudWatchLogsClient(account.AccessKey, account.SecretKey, endpoint);
            _sts = new AmazonSecurityTokenServiceClient(account.AccessKey, account.SecretKey, endpoint);
        }

        private static string Normalize(string name)
        {
            name = name.Trim().ToLowerInvariant();
            name = Regex.Replace(name, @"[^a-z0-9-]", "-");
            name = Regex.Replace(name, @"-+", "-");
            return name.Trim('-');
        }

        private AmazonECSClient GetClient(CloudConnectionSecrets account)
        {
            return new AmazonECSClient(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));
        }

        public async Task<string> CreateCluster(CloudConnectionSecrets account)
        {
            var client = GetClient(account);

            var name = "iwx-cluster";

            var clusters = await client.ListClustersAsync(new ListClustersRequest());

            if (clusters.ClusterArns.Any(x => x.Contains(name)))
                return name;

            var request = new CreateClusterRequest { ClusterName = name };

            await client.CreateClusterAsync(request);

            return name;
        }

        public async Task<string> CreateTaskDefinition(CloudConnectionSecrets account, string image)
        {
            var client = GetClient(account);

            var request = new RegisterTaskDefinitionRequest
            {
                Family = "iwx-task",
                NetworkMode = "awsvpc",
                RequiresCompatibilities = new List<string> { "FARGATE" },
                Cpu = "256",
                Memory = "512",
                ContainerDefinitions = new List<ContainerDefinition>
                {
                    new ContainerDefinition
                    {
                        Name = "iwx-container",
                        Image = image,
                        Essential = true
                    }
                }
            };

            var result = await client.RegisterTaskDefinitionAsync(request);

            return result.TaskDefinition.TaskDefinitionArn;
        }

        public async Task<string> CreateService(CloudConnectionSecrets account, string cluster, string taskDefinition)
        {
            var client = GetClient(account);

            var request = new CreateServiceRequest
            {
                Cluster = cluster,
                ServiceName = "iwx-service",
                TaskDefinition = taskDefinition,
                DesiredCount = 1,
                LaunchType = "FARGATE"
            };

            await client.CreateServiceAsync(request);

            return "Service Created";
        }

        private async global::System.Threading.Tasks.Task TagAsync(string resourceId, string name, string key = "Name", string value = "aws")
        {
            await _ec2.CreateTagsAsync(new CreateTagsRequest
            {
                Resources = new List<string> { resourceId },
                Tags = new List<Ec2Tag>
                {
                    new Ec2Tag
                    {
                        Key = key,
                        Value = key == "Name" ? name : value
                    }
                }
            });
        }
        
        private async Task<string> EnsureVpcAsync(string appName)
        {
            var request = new DescribeVpcsRequest
            {
                Filters = new List<Filter>
                {
                    new Filter("tag:Name", new List<string> { $"iwx-{appName}-vpc" })
                }
            };

            DescribeVpcsResponse response;
            try
            {
                response = await _ec2.DescribeVpcsAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to describe VPCs. Check your AWS credentials and permissions. Details: {ex.Message}", ex);
            }

            // Safeguard against null Vpcs list
            var vpcs = response.Vpcs ?? new List<Vpc>();

            if (vpcs.Any())
                return vpcs[0].VpcId;

            var vpc = await _ec2.CreateVpcAsync(new CreateVpcRequest { CidrBlock = "10.0.0.0/16" });

            await _ec2.ModifyVpcAttributeAsync(new ModifyVpcAttributeRequest
            {
                VpcId = vpc.Vpc.VpcId,
                EnableDnsSupport = true 
            });

            await _ec2.ModifyVpcAttributeAsync(new ModifyVpcAttributeRequest
            {
                VpcId = vpc.Vpc.VpcId,
                EnableDnsHostnames = true 
            });

            await TagAsync(vpc.Vpc.VpcId, $"iwx-{appName}-vpc");

            var igw = await _ec2.CreateInternetGatewayAsync(new CreateInternetGatewayRequest());
            await TagAsync(igw.InternetGateway.InternetGatewayId, $"iwx-{appName}-igw");
            await _ec2.AttachInternetGatewayAsync(new AttachInternetGatewayRequest
            {
                InternetGatewayId = igw.InternetGateway.InternetGatewayId,
                VpcId = vpc.Vpc.VpcId
            });

            var rt = await _ec2.CreateRouteTableAsync(new CreateRouteTableRequest
            {
                VpcId = vpc.Vpc.VpcId
            });
            await TagAsync(rt.RouteTable.RouteTableId, $"iwx-{appName}-rt");

            await _ec2.CreateRouteAsync(new CreateRouteRequest
            {
                RouteTableId = rt.RouteTable.RouteTableId,
                DestinationCidrBlock = "0.0.0.0/0",
                GatewayId = igw.InternetGateway.InternetGatewayId
            });

            return vpc.Vpc.VpcId;
        }
        
        private async Task<List<string>> EnsurePublicSubnetsAsync(string vpcId, string appName)
        {
            var existing = await _ec2.DescribeSubnetsAsync(new DescribeSubnetsRequest
            {
                Filters = new List<Filter>
                {
                    new Filter("vpc-id", new List<string> { vpcId }),
                    new Filter("tag:Purpose", new List<string> { "public" })
                }
            });

            if ((existing.Subnets?.Count ?? 0) >= 2)
                return existing.Subnets.Take(2).Select(x => x.SubnetId).ToList();

            var azs = await _ec2.DescribeAvailabilityZonesAsync(new DescribeAvailabilityZonesRequest());
            var zones = azs.AvailabilityZones.Take(2).Select(x => x.ZoneName).ToList();

            var subnetIds = new List<string>();
            var cidrs = new[] { "10.0.1.0/24", "10.0.2.0/24" };

            for (int i = 0; i < 2; i++)
            {
                var subnet = await _ec2.CreateSubnetAsync(new CreateSubnetRequest
                {
                    VpcId = vpcId,
                    CidrBlock = cidrs[i],
                    AvailabilityZone = zones[Math.Min(i, zones.Count - 1)]
                });

                await TagAsync(subnet.Subnet.SubnetId, $"iwx-{appName}-public-{i + 1}", "Purpose", "public");

                await _ec2.ModifySubnetAttributeAsync(new ModifySubnetAttributeRequest
                {
                    SubnetId = subnet.Subnet.SubnetId,
                    MapPublicIpOnLaunch = true 
                });

                subnetIds.Add(subnet.Subnet.SubnetId);
            }

            return subnetIds;
        }

        private async Task<string> EnsureSecurityGroupAsync(string vpcId, string appName)
        {
            var groups = await _ec2.DescribeSecurityGroupsAsync(
                new DescribeSecurityGroupsRequest
                {
                    Filters = new List<Filter>
                    {
                new Filter("vpc-id", new List<string> { vpcId }),
                new Filter("group-name", new List<string> { $"iwx-{appName}-alb-sg" })
                    }
                });

            var securityGroups = groups?.SecurityGroups ?? new List<SecurityGroup>();

            if (securityGroups.Count > 0)
                return securityGroups[0].GroupId;

            var sg = await _ec2.CreateSecurityGroupAsync(
                new CreateSecurityGroupRequest
                {
                    GroupName = $"iwx-{appName}-alb-sg",
                    Description = "ALB security group",
                    VpcId = vpcId,
                });

            if (sg?.GroupId == null)
                throw new Exception("Failed to create security group");

            var groupId = sg.GroupId;

            try
            {
                await _ec2.AuthorizeSecurityGroupIngressAsync(
                    new AuthorizeSecurityGroupIngressRequest
                    {
                        GroupId = groupId,
                        IpPermissions = new List<IpPermission>
                        {
                        new IpPermission
                        {
                            IpProtocol="tcp",
                            FromPort=80,
                            ToPort=80,
                            Ipv4Ranges=new List<IpRange>
                            {
                                new IpRange{ CidrIp="0.0.0.0/0"}
                            }
                        },
                        new IpPermission
                        {
                            IpProtocol="tcp",
                            FromPort=443,
                            ToPort=443,
                            Ipv4Ranges=new List<IpRange>
                                {
                                    new IpRange{ CidrIp="0.0.0.0/0"}
                                }
                        }
                        }
                    });
            }
            catch (AmazonEC2Exception ex)
            {
                if (!ex.Message.Contains("already exists"))
                    throw;
            }

            catch (Exception ex)
            {
                throw new Exception("Failed: " + ex.Message);
            }

            //await _ec2.AuthorizeSecurityGroupEgressAsync(new AuthorizeSecurityGroupEgressRequest
            //{
            //    GroupId = sg.GroupId,
            //    IpPermissions = new List<IpPermission>
            //    {
            //        new IpPermission
            //        {
            //            IpProtocol = "-1",
            //            Ipv4Ranges = new List<IpRange>
            //            {
            //                new IpRange { CidrIp = "0.0.0.0/0" }
            //            }
            //        }
            //    }
            //});

            return sg.GroupId;
        }

        private async Task<string> EnsureClusterAsync(string appName)
        {
            var clusterName = $"iwx-{appName}-cluster";
            var cluster = await _ecs.DescribeClustersAsync(new DescribeClustersRequest
            {
                Clusters = new List<string> { clusterName }
            });

            if (cluster.Clusters.Any(x => x.ClusterName == clusterName))
                return clusterName;

            await _ecs.CreateClusterAsync(new CreateClusterRequest
            {
                ClusterName = clusterName
            });

            return clusterName;
        }

        private async Task<string> EnsureExecutionRoleAsync(string accountId)
        {
            const string roleName = "ecsTaskExecutionRoleAsync";
            var arn = $"arn:aws:iam::{accountId}:role/{roleName}";

            try
            {
                var role = await _iam.GetRoleAsync(new GetRoleRequest { RoleName = roleName });
                return role.Role.Arn;
            }
            catch
            {
                var trust = @"{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {
                            ""Effect"":""Allow"",
                            ""Principal"": { ""Service"": ""ecs-tasks.amazonaws.com"" },
                            ""Action"": ""sts:AssumeRole""
                        }
                    ]
                }";

                await _iam.CreateRoleAsync(new CreateRoleRequest
                {
                    RoleName = roleName,
                    AssumeRolePolicyDocument = trust
                });

                await _iam.AttachRolePolicyAsync(new AttachRolePolicyRequest
                {
                    RoleName = roleName,
                    PolicyArn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
                });

                return arn;
            }
        }

        private async Task<string> EnsureLogGroupAsync(string appName)
        {
            var name = $"/ecs/iwx-{appName}";
            var groups = await _logs.DescribeLogGroupsAsync(new DescribeLogGroupsRequest
            {
                LogGroupNamePrefix = name
            });

            if (!groups.LogGroups.Any(x => x.LogGroupName == name))
            {
                await _logs.CreateLogGroupAsync(new CreateLogGroupRequest
                {
                    LogGroupName = name
                });
            }

            return name;
        }
        
        private async Task<string> EnsureTargetGroupAsync(string vpcId, string appName)
        {
            var tgName = $"iwx-{appName}-tg".Substring(0, Math.Min(32, $"iwx-{appName}-tg".Length));

            try
            {
                var existing = await _elb.DescribeTargetGroupsAsync(new DescribeTargetGroupsRequest
                {
                    Names = new List<string> { tgName }
                });

                if (existing.TargetGroups.Count > 0)
                    return existing.TargetGroups[0].TargetGroupArn;
            }
            catch (Exception ex)
            {
                
            }

            var tg = await _elb.CreateTargetGroupAsync(new CreateTargetGroupRequest
            {
                Name = tgName,
                Protocol = ProtocolEnum.HTTP,
                Port = 80,
                VpcId = vpcId,
                TargetType = TargetTypeEnum.Ip,
                HealthCheckPath = "/",
                HealthCheckProtocol = ProtocolEnum.HTTP
            });

            return tg.TargetGroups[0].TargetGroupArn;
        }

        private async Task<CreateLoadBalancerResponse> EnsureLoadBalancerAsync(List<string> subnetIds, string sgId, string appName)
        {
            var lbName = $"iwx-{appName}-alb".Substring(0, Math.Min(32, $"iwx-{appName}-alb".Length));

            var existing = await _elb.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest());

            List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> loadBalancers = new List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>();

            if (existing.LoadBalancers != null)
                loadBalancers = existing.LoadBalancers;

            var lb = loadBalancers.FirstOrDefault(x => x.LoadBalancerName == lbName);

            if (lb != null)
            {
                return new CreateLoadBalancerResponse
                {
                    LoadBalancers = new List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> { lb }
                };
            }

            return await _elb.CreateLoadBalancerAsync(new CreateLoadBalancerRequest
            {
                Name = lbName,
                Type = LoadBalancerTypeEnum.Application,
                Scheme = LoadBalancerSchemeEnum.InternetFacing,
                SecurityGroups = new List<string> { sgId },
                Subnets = subnetIds
            });
        }

        private async global::System.Threading.Tasks.Task EnsureListenerAsync(string lbArn, string targetGroupArn)
        {
            var listeners = await _elb.DescribeListenersAsync(new DescribeListenersRequest
            {
                LoadBalancerArn = lbArn
            });

            if (listeners.Listeners != null && listeners.Listeners.Any(x => x.Port == 80))
                return;

            await _elb.CreateListenerAsync(new CreateListenerRequest
            {
                LoadBalancerArn = lbArn,
                Port = 80,
                Protocol = ProtocolEnum.HTTP,
                DefaultActions = new List<Amazon.ElasticLoadBalancingV2.Model.Action>
                {
                    new Amazon.ElasticLoadBalancingV2.Model.Action
                    {
                        Type = ActionTypeEnum.Forward,
                        TargetGroupArn = targetGroupArn
                    }
                }
            });
        }

        public async Task<AwsInfrastructureResult> EnsureInfrastructureAsync(CloudConnectionSecrets account, string appName)
        {
            Init(account);

            var safe = Normalize(appName);
            var accountId = (await _sts.GetCallerIdentityAsync(new GetCallerIdentityRequest())).Account;

            var vpcId = await EnsureVpcAsync(safe);
            var subnetIds = await EnsurePublicSubnetsAsync(vpcId, safe);
            var sgId = await EnsureSecurityGroupAsync(vpcId, safe);
            var clusterName = await EnsureClusterAsync(safe);
            var roleArn = await EnsureExecutionRoleAsync(accountId);
            var logGroupName = await EnsureLogGroupAsync(safe);
            var targetGroupArn = await EnsureTargetGroupAsync(vpcId, safe);
            var lb = await EnsureLoadBalancerAsync(subnetIds, sgId, safe);
            await EnsureListenerAsync(lb.LoadBalancers[0].LoadBalancerArn, targetGroupArn);

            return new AwsInfrastructureResult
            {
                Region = _region,
                VpcId = vpcId,
                PublicSubnetIds = subnetIds,
                SecurityGroupId = sgId,
                ClusterName = clusterName,
                LoadBalancerArn = lb.LoadBalancers[0].LoadBalancerArn,
                LoadBalancerDnsName = lb.LoadBalancers[0].DNSName,
                TargetGroupArn = targetGroupArn,
                ExecutionRoleArn = roleArn,
                LogGroupName = logGroupName,
            };
        }
    }
}
