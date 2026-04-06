using IWX_CloudZen.CloudServices.Cluster.Interfaces;
using IWX_CloudZen.CloudServices.Cluster.DTOs;
using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using IWX_CloudZen.CloudAccounts.DTOs;

namespace IWX_CloudZen.CloudServices.Cluster.Providers
{
    public class AwsClusterProvider : IClusterProvider
    {
        private AmazonECSClient GetClient(CloudConnectionSecrets account)
        {
            return new AmazonECSClient(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));
        }

        public async Task<ClusterListResponse> ListClusters(CloudConnectionSecrets account)
        {
            var client = GetClient(account);

            var response = await client.ListClustersAsync(new ListClustersRequest());

            return new ClusterListResponse { ClusterArns = response.ClusterArns };
        }

        public async Task<ClusterResponse> CreateCluster(CloudConnectionSecrets account, string clusterName)
        {
            var client = GetClient(account);

            var clusters = await client.ListClustersAsync(new ListClustersRequest());

            if (clusters.ClusterArns.Any(x => x.Contains(clusterName)))
                return new ClusterResponse { Name = clusterName, Status = "Already Exists" };

            var request = new Amazon.ECS.Model.CreateClusterRequest { ClusterName = clusterName };

            await client.CreateClusterAsync(request);

            return new ClusterResponse { Name = clusterName, Status = "Created" };
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
