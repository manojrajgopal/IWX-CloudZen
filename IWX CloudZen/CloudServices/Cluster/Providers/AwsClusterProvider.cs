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

        public async Task<List<CloudClusterInfo>> FetchAllClusters(CloudConnectionSecrets account)
        {
            var client = GetClient(account);

            var arnList = new List<string>();
            string? nextToken = null;

            do
            {
                var listResponse = await client.ListClustersAsync(new ListClustersRequest { NextToken = nextToken });
                arnList.AddRange(listResponse.ClusterArns);
                nextToken = listResponse.NextToken;
            }
            while (nextToken != null);

            if (arnList.Count == 0)
                return new List<CloudClusterInfo>();

            var describeResponse = await client.DescribeClustersAsync(new DescribeClustersRequest
            {
                Clusters = arnList,
                Include = new List<string> { "SETTINGS" }
            });

            return describeResponse.Clusters.Select(c => new CloudClusterInfo
            {
                Name = c.ClusterName,
                ClusterArn = c.ClusterArn,
                Status = c.Status,
                ContainerInsightsEnabled = c.Settings
                    .Any(s => s.Name == ClusterSettingName.ContainerInsights && s.Value == "enabled")
            }).ToList();
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

        public async Task<ClusterResponse> UpdateCluster(CloudConnectionSecrets account, string clusterName, bool enableContainerInsights)
        {
            var client = GetClient(account);

            var setting = new ClusterSetting
            {
                Name = ClusterSettingName.ContainerInsights,
                Value = enableContainerInsights ? "enabled" : "disabled"
            };

            var request = new Amazon.ECS.Model.UpdateClusterRequest
            {
                Cluster = clusterName,
                Settings = new List<ClusterSetting> { setting }
            };

            var response = await client.UpdateClusterAsync(request);

            return new ClusterResponse
            {
                Name = response.Cluster.ClusterName,
                Status = response.Cluster.Status
            };
        }

        public async Task<IWX_CloudZen.CloudServices.Cluster.DTOs.DeleteClusterResponse> DeleteCluster(CloudConnectionSecrets account, string clusterName)
        {
            var client = GetClient(account);

            var request = new Amazon.ECS.Model.DeleteClusterRequest { Cluster = clusterName };

            var response = await client.DeleteClusterAsync(request);

            return new IWX_CloudZen.CloudServices.Cluster.DTOs.DeleteClusterResponse
            {
                ClusterArn = response.Cluster.ClusterArn,
                Status = response.Cluster.Status
            };
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
