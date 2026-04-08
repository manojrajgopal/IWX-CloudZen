using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.ECS.DTOs;
using IWX_CloudZen.CloudServices.ECS.Interfaces;
using System.Text.Json;

// Disambiguate conflicting names from Amazon.ECS.Model vs System namespaces
using SysTask    = System.Threading.Tasks.Task;
using AwsEcsTask = Amazon.ECS.Model.Task;
using AwsKvp     = Amazon.ECS.Model.KeyValuePair;

namespace IWX_CloudZen.CloudServices.ECS.Providers
{
    public class AwsEcsProvider : IEcsProvider
    {
        // ================================================================
        // Client
        // ================================================================

        private static AmazonECSClient GetClient(CloudConnectionSecrets account) =>
            new AmazonECSClient(
                account.AccessKey,
                account.SecretKey,
                RegionEndpoint.GetBySystemName(account.Region ?? "us-east-1"));

        // ================================================================
        // Task Definitions
        // ================================================================

        public async Task<List<CloudTaskDefinitionInfo>> FetchAllTaskDefinitions(
            CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var arns = new List<string>();
            string? nextToken = null;

            // List only ACTIVE task definitions
            do
            {
                var listResponse = await client.ListTaskDefinitionsAsync(new ListTaskDefinitionsRequest
                {
                    Status = TaskDefinitionStatus.ACTIVE,
                    NextToken = nextToken
                });
                arns.AddRange(listResponse.TaskDefinitionArns);
                nextToken = listResponse.NextToken;
            }
            while (nextToken != null);

            var result = new List<CloudTaskDefinitionInfo>();

            // DescribeTaskDefinition does not support batch calls — describe individually
            foreach (var arn in arns)
            {
                try
                {
                    var describeResponse = await client.DescribeTaskDefinitionAsync(
                        new DescribeTaskDefinitionRequest { TaskDefinition = arn });
                    result.Add(MapTaskDefinition(describeResponse.TaskDefinition));
                }
                catch
                {
                    // Skip any task definition that can no longer be described
                }
            }

            return result;
        }

        public async Task<CloudTaskDefinitionInfo> RegisterTaskDefinition(
            CloudConnectionSecrets account,
            DTOs.RegisterTaskDefinitionRequest request)
        {
            var client = GetClient(account);

            var containerDefs = BuildContainerDefinitions(request.ContainerDefinitions);

            var awsRequest = new Amazon.ECS.Model.RegisterTaskDefinitionRequest
            {
                Family = request.Family,
                Cpu = request.Cpu,
                Memory = request.Memory,
                NetworkMode = new NetworkMode(request.NetworkMode),
                ExecutionRoleArn = string.IsNullOrWhiteSpace(request.ExecutionRoleArn)
                    ? null : request.ExecutionRoleArn,
                TaskRoleArn = string.IsNullOrWhiteSpace(request.TaskRoleArn)
                    ? null : request.TaskRoleArn,
                RequiresCompatibilities = request.RequiresCompatibilities,
                ContainerDefinitions = containerDefs
            };

            if (!string.IsNullOrWhiteSpace(request.OsFamily))
            {
                awsRequest.RuntimePlatform = new RuntimePlatform
                {
                    OperatingSystemFamily = new OSFamily(request.OsFamily)
                };
            }

            var response = await client.RegisterTaskDefinitionAsync(awsRequest);
            return MapTaskDefinition(response.TaskDefinition);
        }

        public async Task<CloudTaskDefinitionInfo> DeregisterTaskDefinition(
            CloudConnectionSecrets account, string taskDefinitionArn)
        {
            var client = GetClient(account);

            var response = await client.DeregisterTaskDefinitionAsync(
                new DeregisterTaskDefinitionRequest { TaskDefinition = taskDefinitionArn });

            return MapTaskDefinition(response.TaskDefinition);
        }

        public async SysTask DeleteTaskDefinition(
            CloudConnectionSecrets account, string taskDefinitionArn)
        {
            var client = GetClient(account);

            // Task definitions must be INACTIVE before deletion
            await client.DeleteTaskDefinitionsAsync(new DeleteTaskDefinitionsRequest
            {
                TaskDefinitions = new List<string> { taskDefinitionArn }
            });
        }

        // ================================================================
        // Services
        // ================================================================

        public async Task<List<CloudEcsServiceInfo>> FetchAllServices(
            CloudConnectionSecrets account, string clusterName)
        {
            var client = GetClient(account);
            var arns = new List<string>();
            string? nextToken = null;

            do
            {
                var listResponse = await client.ListServicesAsync(new ListServicesRequest
                {
                    Cluster = clusterName,
                    NextToken = nextToken
                });
                arns.AddRange(listResponse.ServiceArns);
                nextToken = listResponse.NextToken;
            }
            while (nextToken != null);

            if (arns.Count == 0)
                return new List<CloudEcsServiceInfo>();

            var result = new List<CloudEcsServiceInfo>();

            // DescribeServices supports up to 10 per call
            foreach (var batch in arns.Chunk(10))
            {
                var describeResponse = await client.DescribeServicesAsync(new DescribeServicesRequest
                {
                    Cluster = clusterName,
                    Services = batch.ToList()
                });

                result.AddRange(describeResponse.Services.Select(s => MapService(s, clusterName)));
            }

            return result;
        }

        public async Task<CloudEcsServiceInfo> CreateService(
            CloudConnectionSecrets account, CreateEcsServiceRequest request)
        {
            var client = GetClient(account);

            var awsRequest = new Amazon.ECS.Model.CreateServiceRequest
            {
                ServiceName = request.ServiceName,
                Cluster = request.ClusterName,
                TaskDefinition = request.TaskDefinition,
                DesiredCount = request.DesiredCount,
                LaunchType = new LaunchType(request.LaunchType),
                SchedulingStrategy = new SchedulingStrategy(request.SchedulingStrategy)
            };

            if (request.NetworkConfiguration != null)
            {
                awsRequest.NetworkConfiguration = BuildNetworkConfiguration(request.NetworkConfiguration);
            }

            var response = await client.CreateServiceAsync(awsRequest);
            return MapService(response.Service, request.ClusterName);
        }

        public async Task<CloudEcsServiceInfo> UpdateService(
            CloudConnectionSecrets account,
            string clusterName,
            string serviceName,
            int? desiredCount,
            string? taskDefinition)
        {
            var client = GetClient(account);

            var awsRequest = new UpdateServiceRequest
            {
                Cluster = clusterName,
                Service = serviceName
            };

            if (desiredCount.HasValue)
                awsRequest.DesiredCount = desiredCount.Value;

            if (!string.IsNullOrWhiteSpace(taskDefinition))
                awsRequest.TaskDefinition = taskDefinition;

            var response = await client.UpdateServiceAsync(awsRequest);
            return MapService(response.Service, clusterName);
        }

        public async SysTask DeleteService(
            CloudConnectionSecrets account, string clusterName, string serviceName)
        {
            var client = GetClient(account);

            // Must scale to 0 before deletion
            await client.UpdateServiceAsync(new UpdateServiceRequest
            {
                Cluster = clusterName,
                Service = serviceName,
                DesiredCount = 0
            });

            await client.DeleteServiceAsync(new DeleteServiceRequest
            {
                Cluster = clusterName,
                Service = serviceName,
                Force = true
            });
        }

        // ================================================================
        // Tasks
        // ================================================================

        public async Task<List<CloudEcsTaskInfo>> FetchAllTasks(
            CloudConnectionSecrets account, string clusterName)
        {
            var client = GetClient(account);
            var arns = new List<string>();

            // Fetch running tasks
            string? nextToken = null;
            do
            {
                var listResponse = await client.ListTasksAsync(new ListTasksRequest
                {
                    Cluster = clusterName,
                    DesiredStatus = DesiredStatus.RUNNING,
                    NextToken = nextToken
                });
                arns.AddRange(listResponse.TaskArns);
                nextToken = listResponse.NextToken;
            }
            while (nextToken != null);

            // Fetch stopped tasks (recent history)
            nextToken = null;
            do
            {
                var listResponse = await client.ListTasksAsync(new ListTasksRequest
                {
                    Cluster = clusterName,
                    DesiredStatus = DesiredStatus.STOPPED,
                    NextToken = nextToken
                });
                arns.AddRange(listResponse.TaskArns);
                nextToken = listResponse.NextToken;
            }
            while (nextToken != null);

            if (arns.Count == 0)
                return new List<CloudEcsTaskInfo>();

            var result = new List<CloudEcsTaskInfo>();

            // DescribeTasks supports up to 100 per call
            foreach (var batch in arns.Chunk(100))
            {
                var describeResponse = await client.DescribeTasksAsync(new DescribeTasksRequest
                {
                    Cluster = clusterName,
                    Tasks = batch.ToList()
                });
                result.AddRange(describeResponse.Tasks.Select(t => MapTask(t, clusterName)));
            }

            return result;
        }

        public async System.Threading.Tasks.Task<List<CloudEcsTaskInfo>> RunTask(
            CloudConnectionSecrets account, IWX_CloudZen.CloudServices.ECS.DTOs.RunTaskRequest request)
        {
            var client = GetClient(account);

            var awsRequest = new Amazon.ECS.Model.RunTaskRequest
            {
                Cluster = request.ClusterName,
                TaskDefinition = request.TaskDefinition,
                LaunchType = new LaunchType(request.LaunchType),
                Count = request.Count
            };

            if (request.NetworkConfiguration != null)
            {
                awsRequest.NetworkConfiguration = BuildNetworkConfiguration(request.NetworkConfiguration);
            }

            if (request.EnvironmentOverrides?.Count > 0)
            {
                awsRequest.Overrides = new TaskOverride
                {
                    ContainerOverrides = request.EnvironmentOverrides
                        .Select(o => new ContainerOverride
                        {
                            Name = o.ContainerName,
                            Environment = o.Environment
                                .Select(e => new AwsKvp { Name = e.Name, Value = e.Value })
                                .ToList()
                        })
                        .ToList()
                };
            }

            var response = await client.RunTaskAsync(awsRequest);

            if (response.Failures?.Count > 0)
            {
                var reasons = string.Join("; ", response.Failures.Select(f =>
                    $"{f.Arn ?? "unknown"}: {f.Reason}"));
                throw new InvalidOperationException($"ECS RunTask failures: {reasons}");
            }

            return response.Tasks.Select(t => MapTask(t, request.ClusterName)).ToList();
        }

        public async SysTask StopTask(
            CloudConnectionSecrets account, string clusterName, string taskArn, string? reason)
        {
            var client = GetClient(account);

            await client.StopTaskAsync(new Amazon.ECS.Model.StopTaskRequest
            {
                Cluster = clusterName,
                Task = taskArn,
                Reason = reason ?? "Stopped by IWX CloudZen"
            });
        }

        // ================================================================
        // Private Helpers
        // ================================================================

        private static Amazon.ECS.Model.NetworkConfiguration BuildNetworkConfiguration(
            NetworkConfigurationDto dto) =>
            new()
            {
                AwsvpcConfiguration = new AwsVpcConfiguration
                {
                    Subnets = dto.Subnets,
                    SecurityGroups = dto.SecurityGroups,
                    AssignPublicIp = dto.AssignPublicIp
                        ? AssignPublicIp.ENABLED
                        : AssignPublicIp.DISABLED
                }
            };

        private static List<ContainerDefinition> BuildContainerDefinitions(
            List<ContainerDefinitionDto> dtos)
        {
            return dtos.Select(c =>
            {
                var def = new ContainerDefinition
                {
                    Name = c.Name,
                    Image = c.Image,
                    Essential = c.Essential
                };

                if (c.Cpu.HasValue) def.Cpu = c.Cpu.Value;
                if (c.Memory.HasValue) def.Memory = c.Memory.Value;
                if (c.MemoryReservation.HasValue) def.MemoryReservation = c.MemoryReservation.Value;

                if (c.PortMappings?.Count > 0)
                {
                    def.PortMappings = c.PortMappings.Select(p => new PortMapping
                    {
                        ContainerPort = p.ContainerPort,
                        HostPort = p.HostPort ?? 0,
                        Protocol = p.Protocol?.Equals("udp", StringComparison.OrdinalIgnoreCase) == true
                            ? TransportProtocol.Udp
                            : TransportProtocol.Tcp
                    }).ToList();
                }

                if (c.Environment?.Count > 0)
                {
                    def.Environment = c.Environment
                        .Select(e => new AwsKvp { Name = e.Name, Value = e.Value })
                        .ToList();
                }

                if (c.LogConfiguration != null)
                {
                    def.LogConfiguration = new LogConfiguration
                    {
                        LogDriver = new LogDriver(c.LogConfiguration.LogDriver),
                        Options = c.LogConfiguration.Options
                    };
                }

                return def;
            }).ToList();
        }

        // ================================================================
        // Mappers
        // ================================================================

        private static CloudTaskDefinitionInfo MapTaskDefinition(TaskDefinition td) => new()
        {
            Family = td.Family,
            TaskDefinitionArn = td.TaskDefinitionArn,
            Revision = td.Revision.GetValueOrDefault(),
            Status = td.Status?.Value ?? "ACTIVE",
            Cpu = td.Cpu ?? "256",
            Memory = td.Memory ?? "512",
            NetworkMode = td.NetworkMode?.Value ?? "awsvpc",
            ExecutionRoleArn = td.ExecutionRoleArn,
            TaskRoleArn = td.TaskRoleArn,
            RequiresCompatibilities = td.RequiresCompatibilities?.Count > 0
                ? string.Join(",", td.RequiresCompatibilities)
                : "FARGATE",
            OsFamily = td.RuntimePlatform?.OperatingSystemFamily?.Value,
            ContainerCount = td.ContainerDefinitions?.Count ?? 0,
            ContainerDefinitionsJson = td.ContainerDefinitions?.Count > 0
                ? JsonSerializer.Serialize(td.ContainerDefinitions.Select(c => new
                {
                    c.Name,
                    c.Image,
                    c.Cpu,
                    c.Memory,
                    c.Essential,
                    PortMappings = c.PortMappings?.Select(p => new
                    {
                        p.ContainerPort,
                        p.HostPort,
                        Protocol = p.Protocol?.Value
                    }),
                    Environment = c.Environment?.Select(e => new { e.Name, e.Value })
                }))
                : null
        };

        private static CloudEcsServiceInfo MapService(Service s, string clusterName) => new()
        {
            ServiceName = s.ServiceName,
            ServiceArn = s.ServiceArn,
            ClusterName = clusterName,
            ClusterArn = s.ClusterArn,
            TaskDefinition = s.TaskDefinition,
            DesiredCount = s.DesiredCount.GetValueOrDefault(),
            RunningCount = s.RunningCount.GetValueOrDefault(),
            PendingCount = s.PendingCount.GetValueOrDefault(),
            Status = s.Status ?? string.Empty,
            LaunchType = s.LaunchType?.Value ?? "FARGATE",
            SchedulingStrategy = s.SchedulingStrategy?.Value ?? "REPLICA",
            NetworkConfigurationJson = s.NetworkConfiguration?.AwsvpcConfiguration != null
                ? JsonSerializer.Serialize(new
                {
                    Subnets = s.NetworkConfiguration.AwsvpcConfiguration.Subnets,
                    SecurityGroups = s.NetworkConfiguration.AwsvpcConfiguration.SecurityGroups,
                    AssignPublicIp = s.NetworkConfiguration.AwsvpcConfiguration.AssignPublicIp?.Value
                })
                : null,
            ServiceCreatedAt = s.CreatedAt
        };

        private static CloudEcsTaskInfo MapTask(AwsEcsTask t, string clusterName) => new()
        {
            TaskArn = t.TaskArn,
            ClusterName = clusterName,
            ClusterArn = t.ClusterArn,
            TaskDefinitionArn = t.TaskDefinitionArn,
            Group = t.Group,
            LastStatus = t.LastStatus ?? string.Empty,
            DesiredStatus = t.DesiredStatus ?? string.Empty,
            Cpu = t.Cpu,
            Memory = t.Memory,
            LaunchType = t.LaunchType?.Value ?? "FARGATE",
            Connectivity = t.Connectivity?.Value,
            StopCode = t.StopCode?.Value,
            StoppedReason = t.StoppedReason,
            StartedAt = t.StartedAt == default ? null : t.StartedAt,
            StoppedAt = t.StoppedAt == default ? null : t.StoppedAt,
            PullStartedAt = t.PullStartedAt == default ? null : t.PullStartedAt,
            PullStoppedAt = t.PullStoppedAt == default ? null : t.PullStoppedAt
        };
    }
}
