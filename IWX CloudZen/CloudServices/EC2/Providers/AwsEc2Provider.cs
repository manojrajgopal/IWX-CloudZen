using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.EC2.DTOs;
using IWX_CloudZen.CloudServices.EC2.Interfaces;

namespace IWX_CloudZen.CloudServices.EC2.Providers
{
    public class AwsEc2Provider : IEc2Provider
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

        private static CloudEc2InstanceInfo MapInstance(Instance instance) => new()
        {
            InstanceId = instance.InstanceId,
            Name = GetNameTag(instance.Tags),
            InstanceType = instance.InstanceType?.Value ?? string.Empty,
            State = instance.State?.Name?.Value ?? string.Empty,
            PublicIpAddress = instance.PublicIpAddress ?? string.Empty,
            PrivateIpAddress = instance.PrivateIpAddress ?? string.Empty,
            VpcId = instance.VpcId ?? string.Empty,
            SubnetId = instance.SubnetId ?? string.Empty,
            ImageId = instance.ImageId ?? string.Empty,
            KeyName = instance.KeyName ?? string.Empty,
            Architecture = instance.Architecture?.Value ?? string.Empty,
            Platform = instance.PlatformDetails ?? string.Empty,
            Monitoring = instance.Monitoring?.State?.Value ?? string.Empty,
            EbsOptimized = instance.EbsOptimized ?? false,
            SecurityGroups = instance.SecurityGroups?.Select(sg => new Ec2SecurityGroupDto
            {
                GroupId = sg.GroupId,
                GroupName = sg.GroupName
            }).ToList() ?? new(),
            Tags = instance.Tags?
                .Where(t => t.Key != "Name")
                .ToDictionary(t => t.Key, t => t.Value) ?? new(),
            LaunchTime = instance.LaunchTime
        };

        // ---- Interface implementation ----

        public async Task<List<CloudEc2InstanceInfo>> FetchAllInstances(CloudConnectionSecrets account)
        {
            var client = GetClient(account);
            var result = new List<CloudEc2InstanceInfo>();
            string? nextToken = null;

            do
            {
                var response = await client.DescribeInstancesAsync(new DescribeInstancesRequest
                {
                    NextToken = nextToken
                });

                foreach (var reservation in response.Reservations)
                {
                    foreach (var instance in reservation.Instances)
                    {
                        // Skip terminated instances
                        if (instance.State?.Name?.Value == "terminated")
                            continue;

                        result.Add(MapInstance(instance));
                    }
                }

                nextToken = response.NextToken;
            }
            while (nextToken != null);

            return result;
        }

        public async Task<CloudEc2InstanceInfo> GetInstance(CloudConnectionSecrets account, string instanceId)
        {
            var client = GetClient(account);

            var response = await client.DescribeInstancesAsync(new DescribeInstancesRequest
            {
                InstanceIds = [instanceId]
            });

            var instance = response.Reservations
                .SelectMany(r => r.Instances)
                .FirstOrDefault()
                ?? throw new KeyNotFoundException($"EC2 instance '{instanceId}' not found.");

            return MapInstance(instance);
        }

        public async Task<List<CloudEc2InstanceInfo>> LaunchInstances(
            CloudConnectionSecrets account,
            string instanceName,
            string imageId,
            string instanceType,
            string? keyName,
            string? subnetId,
            List<string>? securityGroupIds,
            int minCount,
            int maxCount,
            bool ebsOptimized,
            string? userData,
            Dictionary<string, string>? tags)
        {
            var client = GetClient(account);

            var request = new RunInstancesRequest
            {
                ImageId = imageId,
                InstanceType = InstanceType.FindValue(instanceType),
                MinCount = minCount,
                MaxCount = maxCount,
                EbsOptimized = ebsOptimized
            };

            if (!string.IsNullOrWhiteSpace(keyName))
                request.KeyName = keyName;

            if (!string.IsNullOrWhiteSpace(subnetId))
                request.SubnetId = subnetId;

            if (securityGroupIds is { Count: > 0 })
                request.SecurityGroupIds = securityGroupIds;

            if (!string.IsNullOrWhiteSpace(userData))
                request.UserData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userData));

            // Build tag specifications
            var allTags = new List<Tag>
            {
                new Tag { Key = "Name", Value = instanceName }
            };

            if (tags is { Count: > 0 })
            {
                foreach (var kvp in tags)
                {
                    if (kvp.Key != "Name")
                        allTags.Add(new Tag { Key = kvp.Key, Value = kvp.Value });
                }
            }

            request.TagSpecifications =
            [
                new TagSpecification
                {
                    ResourceType = ResourceType.Instance,
                    Tags = allTags
                }
            ];

            var response = await client.RunInstancesAsync(request);

            return response.Reservation.Instances.Select(MapInstance).ToList();
        }

        public async Task<CloudEc2InstanceInfo> UpdateInstance(
            CloudConnectionSecrets account,
            string instanceId,
            string? instanceName,
            string? instanceType,
            List<string>? securityGroupIds,
            Dictionary<string, string>? tags)
        {
            var client = GetClient(account);

            // Update tags (including Name)
            var tagsToSet = new List<Tag>();

            if (!string.IsNullOrWhiteSpace(instanceName))
                tagsToSet.Add(new Tag { Key = "Name", Value = instanceName });

            if (tags is { Count: > 0 })
            {
                foreach (var kvp in tags)
                {
                    if (kvp.Key != "Name")
                        tagsToSet.Add(new Tag { Key = kvp.Key, Value = kvp.Value });
                }
            }

            if (tagsToSet.Count > 0)
            {
                await client.CreateTagsAsync(new CreateTagsRequest
                {
                    Resources = [instanceId],
                    Tags = tagsToSet
                });
            }

            // Update instance type (requires instance to be stopped)
            if (!string.IsNullOrWhiteSpace(instanceType))
            {
                await client.ModifyInstanceAttributeAsync(new ModifyInstanceAttributeRequest
                {
                    InstanceId = instanceId,
                    InstanceType = InstanceType.FindValue(instanceType)
                });
            }

            // Update security groups
            if (securityGroupIds is { Count: > 0 })
            {
                await client.ModifyInstanceAttributeAsync(new ModifyInstanceAttributeRequest
                {
                    InstanceId = instanceId,
                    Groups = securityGroupIds
                });
            }

            // Describe and return updated state
            return await GetInstance(account, instanceId);
        }

        public async Task StartInstance(CloudConnectionSecrets account, string instanceId)
        {
            var client = GetClient(account);
            await client.StartInstancesAsync(new StartInstancesRequest
            {
                InstanceIds = [instanceId]
            });
        }

        public async Task StopInstance(CloudConnectionSecrets account, string instanceId, bool force = false)
        {
            var client = GetClient(account);
            await client.StopInstancesAsync(new StopInstancesRequest
            {
                InstanceIds = [instanceId],
                Force = force
            });
        }

        public async Task RebootInstance(CloudConnectionSecrets account, string instanceId)
        {
            var client = GetClient(account);
            await client.RebootInstancesAsync(new RebootInstancesRequest
            {
                InstanceIds = [instanceId]
            });
        }

        public async Task TerminateInstance(CloudConnectionSecrets account, string instanceId)
        {
            var client = GetClient(account);
            await client.TerminateInstancesAsync(new TerminateInstancesRequest
            {
                InstanceIds = [instanceId]
            });
        }
    }
}
