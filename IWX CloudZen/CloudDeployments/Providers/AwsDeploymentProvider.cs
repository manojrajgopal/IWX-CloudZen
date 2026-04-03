using Amazon;
using Amazon.ApplicationAutoScaling;
using Amazon.ApplicationAutoScaling.Model;
using Amazon.EC2.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudDeployments.DTOs;
using IWX_CloudZen.CloudDeployments.Interfaces;
using IWX_CloudZen.CloudDeployments.Pipeline;
using IWX_CloudZen.CloudServiceCreation.Providers;
using System.Text.RegularExpressions;
using Task = System.Threading.Tasks.Task;

namespace IWX_CloudZen.CloudDeployments.Providers
{
    public class AwsDeploymentProvider : ICloudDeploymentProvider
    {
        private AmazonECSClient _ecs = null!;
        private AmazonECRClient ecr = null!;
        private AmazonElasticLoadBalancingV2Client elb;
        private string region;
        private string _region = string.Empty;

        private readonly AwsServiceCreator _infra = new();
        private readonly EcrPushService _ecrPush = new();
        //private CloudConnectionSecrets account;

        private void Init(CloudConnectionSecrets account)
        {
            //this.account = account;

            _region = account.Region ?? throw new Exception("AWS Region is required.");
            var endpoint = RegionEndpoint.GetBySystemName(_region);
            _ecs = new AmazonECSClient(account.AccessKey, account.SecretKey, endpoint);
            ecr = new AmazonECRClient(account.AccessKey, account.SecretKey, endpoint);
            elb = new AmazonElasticLoadBalancingV2Client(account.AccessKey, account.SecretKey, endpoint);
        }
        
        private string NormalizeAwsName(string name)
        {
            name = name.Trim().ToLowerInvariant();
            name = Regex.Replace(name, @"[^a-z0-9._/-]", "-");
            name = Regex.Replace(name, @"[-._/]{2,}", "-");
            return name.Trim('-', '.', '_', '/');
        }

        private async Task<string> CreateEcrRepo(string name)
        {
            var repoName = NormalizeAwsName(name);
            var repos = await ecr.DescribeRepositoriesAsync(new DescribeRepositoriesRequest());

            var existing = repos.Repositories.FirstOrDefault(x => x.RepositoryName == repoName);

            if (existing != null)
                return existing.RepositoryUri;

            var created = await ecr.CreateRepositoryAsync(new CreateRepositoryRequest { RepositoryName = repoName });

            return created.Repository.RepositoryUri;
        }

        private async Task<string> GetAccountId(CloudConnectionSecrets account)
        {
            var sts = new AmazonSecurityTokenServiceClient(
                    account.AccessKey,
                    account.SecretKey,
                    RegionEndpoint.GetBySystemName(account.Region)
                );

            var identity = await sts.GetCallerIdentityAsync(new GetCallerIdentityRequest());

            return identity.Account;
        }

        private async Task<string> CreateTaskDefinition(CloudConnectionSecrets account, string name, string image)
        {
            var accountId = await GetAccountId(account);
            var executionRoleArn = $"arn:aws:iam::{accountId}:role/ecsTaskExecutionRole";
            var request = new RegisterTaskDefinitionRequest
            {
                Family = name,
                RequiresCompatibilities = new List<string> { "FARGATE" },
                Cpu = "256",
                Memory = "512",
                NetworkMode = "awsvpc",
                ExecutionRoleArn = executionRoleArn,
                ContainerDefinitions = new List<ContainerDefinition>
                {
                    new ContainerDefinition
                    {
                        Name = name,
                        Image = image,
                        Essential = true,
                        PortMappings = new List<PortMapping>
                        {
                            new PortMapping
                            {
                                ContainerPort = 80,
                                Protocol = "tcp"
                            }
                        }
                    }
                }
            };

            var result = await _ecs.RegisterTaskDefinitionAsync(request);

            return result.TaskDefinition.TaskDefinitionArn;
        }

        private async Task<string> EnsureCluster()
        {
            var clusters = await _ecs.ListClustersAsync( new ListClustersRequest());
            var existing = clusters.ClusterArns.FirstOrDefault(x => x.Contains("iwx"));

            if (existing != null)
                return "iwx";

            await _ecs.CreateClusterAsync(new CreateClusterRequest { ClusterName = "iwx" });

            return "iwx";
        }

        private async Task<string> CreateService(string cluster, string task, string name)
        {
            var request = new CreateServiceRequest
            {
                Cluster = cluster,
                ServiceName = name,
                TaskDefinition = task,
                DesiredCount = 1,
                LaunchType = "FARGATE"
            };

            await _ecs.CreateServiceAsync(request);

            return name;
        }

        private async Task<string> CreateLoadBalancer(string name)
        {
            var lb = await elb.CreateLoadBalancerAsync(new CreateLoadBalancerRequest
            {
                Name = name,
                Type = LoadBalancerTypeEnum.Application,
                Subnets = new List<string>
                {
                    "subnet-id"
                }
            });

            return lb.LoadBalancers[0].DNSName;
        }

        private async Task<string> CreateTaskDefinitionAsync(CloudConnectionSecrets account, string name, string image, string executionRoleArn, string logGroupName)
        {
            var request = new RegisterTaskDefinitionRequest
            {
                Family = $"iwx-{name}",
                RequiresCompatibilities = new List<string> { "FARGATE" },
                Cpu = "256",
                Memory = "512",
                NetworkMode = NetworkMode.Awsvpc,
                ExecutionRoleArn = executionRoleArn,
                ContainerDefinitions = new List<ContainerDefinition>
                {
                    new ContainerDefinition
                    {
                        Name = name,
                        Image = image,
                        Essential = true,
                        PortMappings = new List<PortMapping>
                        {
                            new PortMapping
                            {
                                ContainerPort = 80,
                                HostPort = 80,
                                Protocol = TransportProtocol.Tcp
                            }
                        },
                        LogConfiguration = new LogConfiguration
                        {
                            LogDriver = LogDriver.Awslogs,
                            Options = new Dictionary<string, string>
                            {
                                ["awslogs-group"] = logGroupName,
                                ["awslogs-region"] = account.Region ?? _region,
                                ["awslogs-stream-prefix"] = "ecs"
                            }
                        }
                    }
                }
            };

            var result = await _ecs.RegisterTaskDefinitionAsync(request);
            return result.TaskDefinition.TaskDefinitionArn;
        }

        private async Task CreateOrUpdateServiceAsync(
            string cluster, string serviceName, string taskDefinitionArn, List<string> subnetIds, string securityGroupId, string targetGroupArn)
        {
            var describe = await _ecs.DescribeServicesAsync(new DescribeServicesRequest
            {
                Cluster = cluster,
                Services = new List<string> { serviceName }
            });

            var network = new NetworkConfiguration
            {
                AwsvpcConfiguration = new AwsVpcConfiguration
                {
                    AssignPublicIp = AssignPublicIp.ENABLED,
                    SecurityGroups = new List<string> { securityGroupId },
                    Subnets = subnetIds
                }
            };

            var loadBalancers = new List<Amazon.ECS.Model.LoadBalancer>
            {
                new Amazon.ECS.Model.LoadBalancer
                {
                    ContainerName = serviceName,
                    ContainerPort = 80,
                    TargetGroupArn = targetGroupArn
                }
            };

            if (describe.Services.Any(x => x.ServiceName == serviceName))
            {
                await _ecs.UpdateServiceAsync(new UpdateServiceRequest
                {
                    Cluster = cluster,
                    Service = serviceName,
                    TaskDefinition = taskDefinitionArn,
                    DesiredCount = 1,
                    ForceNewDeployment = true,
                    NetworkConfiguration = network
                });
                return;
            }

            await _ecs.CreateServiceAsync(new CreateServiceRequest
            {
                Cluster = cluster,
                ServiceName = serviceName,
                TaskDefinition = taskDefinitionArn,
                DesiredCount = 1,
                LaunchType = LaunchType.FARGATE,
                NetworkConfiguration = network,
                LoadBalancers = loadBalancers
            });
        }

        public async Task<AwsDeploymentResult> Deploy(CloudConnectionSecrets account, IFormFile package, string name, string deploymentType)
        {
            Init(account);

            var safe = NormalizeAwsName(name);
            var infra = await _infra.EnsureInfrastructureAsync(account, safe);
            var (repoUri, _) = await _ecrPush.BuildAndPushAsync(account, package, deploymentType, safe);

            var image = $"{repoUri}:latest";
            var taskArn = await CreateTaskDefinitionAsync(account, safe, image, infra.ExecutionRoleArn, infra.LogGroupName);
            
            await CreateOrUpdateServiceAsync(
                cluster: infra.ClusterName,
                serviceName: safe,
                taskDefinitionArn: taskArn,
                subnetIds: infra.PublicSubnetIds,
                securityGroupId: infra.SecurityGroupId,
                targetGroupArn: infra.TargetGroupArn);

            return new AwsDeploymentResult
            {
                Status = "Running",
                ImageUrl = image,
                ServiceName = safe,
                ClusterName = infra.ClusterName,
                HealthUrl = $"http://{infra.LoadBalancerDnsName}",
                LogsGroup = infra.LogGroupName
            };
        }

        public async Task Stop(CloudConnectionSecrets account, string deploymentName)
        {
            Init(account);

            await _ecs.UpdateServiceAsync(new UpdateServiceRequest
            {
                Cluster = "iwx",
                Service = deploymentName,
                DesiredCount = 0
            });
        }

        public async Task Restart(CloudConnectionSecrets account, string deploymentName)
        {
            Init(account);

            await _ecs.UpdateServiceAsync(new UpdateServiceRequest
            {
                Cluster = "iwx",
                Service = deploymentName,
                DesiredCount = 1
            });
        }

        private async Task EnableScaling(string service)
        {
            var scaling = new AmazonApplicationAutoScalingClient();

            await scaling.RegisterScalableTargetAsync(
                    new RegisterScalableTargetRequest
                    {
                        ServiceNamespace = ServiceNamespace.Ecs,
                        ResourceId = "service/iwx/" + service,
                        MinCapacity = 1,
                        MaxCapacity = 5
                    }
                );
        }
    }
}
