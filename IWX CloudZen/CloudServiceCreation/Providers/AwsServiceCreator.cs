using IWX_CloudZen.CloudServiceCreation.Interfaces;
using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudServiceCreation.Providers
{
    public class AwsServiceCreator : ICloudServiceCreator
    {
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
    }
}
