using System.Text.Json;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudServices.Mapped.DTOs;
using IWX_CloudZen.CloudServices.Mapped.Factory;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.CloudServices.Mapped.Services
{
    public class MappedService
    {
        private readonly CloudAccountService _accounts;
        private readonly AppDbContext _db;

        public MappedService(CloudAccountService accounts, AppDbContext db)
        {
            _accounts = accounts;
            _db = db;
        }

        // ==================================================================
        //  1. FULL RESOURCE GRAPH (from DB — fast, no cloud API calls)
        // ==================================================================

        public async Task<ResourceGraphResponse> GetFullGraph(string user, int accountId)
        {
            var allResources = await LoadAllResourcesFromDb(user, accountId);
            var edges = BuildEdgesFromDb(allResources);
            var graph = BuildTreeFromEdges(allResources, edges);

            var summary = allResources.GroupBy(r => r.ResourceType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ResourceGraphResponse
            {
                Graph = graph,
                Edges = edges,
                Summary = summary,
                TotalResources = allResources.Count
            };
        }

        // ==================================================================
        //  2. VPC RESOURCE TREE (everything inside a VPC)
        // ==================================================================

        public async Task<VpcResourceTreeResponse> GetVpcResourceTree(string user, int accountId, string vpcId)
        {
            var allResources = await LoadAllResourcesFromDb(user, accountId);
            var edges = BuildEdgesFromDb(allResources);

            // Filter to only resources related to this VPC
            var vpcResourceIds = new HashSet<string> { vpcId };
            var vpcEdges = edges.Where(e =>
                e.SourceId == vpcId || e.TargetId == vpcId ||
                vpcResourceIds.Contains(e.SourceId) || vpcResourceIds.Contains(e.TargetId)
            ).ToList();

            // Collect all resource IDs in this VPC's edges
            foreach (var edge in vpcEdges)
            {
                vpcResourceIds.Add(edge.SourceId);
                vpcResourceIds.Add(edge.TargetId);
            }

            var vpcResources = allResources.Where(r =>
                r.ResourceId == vpcId ||
                r.ParentResourceId == vpcId ||
                vpcResourceIds.Contains(r.ResourceId)
            ).ToList();

            // Also include resources whose parent is in the VPC (e.g., EC2 in a Subnet in this VPC)
            var subnetIds = vpcResources.Where(r => r.ResourceType == "Subnet").Select(r => r.ResourceId).ToHashSet();
            var moreResources = allResources.Where(r =>
                !vpcResources.Any(v => v.ResourceId == r.ResourceId) &&
                (subnetIds.Contains(r.ParentResourceId ?? ""))
            ).ToList();
            vpcResources.AddRange(moreResources);

            // Rebuild edges for this subset
            var finalEdges = BuildEdgesFromResources(vpcResources);

            var vpcRecord = allResources.FirstOrDefault(r => r.ResourceId == vpcId);
            var vpcNode = BuildVpcTree(vpcId, vpcRecord?.Name ?? vpcId, vpcResources, finalEdges);

            var hasIgw = vpcResources.Any(r => r.ResourceType == "InternetGateway");

            var summary = vpcResources.Where(r => r.ResourceType != "VPC")
                .GroupBy(r => r.ResourceType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new VpcResourceTreeResponse
            {
                VpcTree = vpcNode,
                AllResources = vpcResources,
                Edges = finalEdges,
                Summary = summary,
                HasInternetGateway = hasIgw,
                TotalResources = vpcResources.Count
            };
        }

        // ==================================================================
        //  3. RESOURCE DEPENDENCIES (parents + children of a single resource)
        // ==================================================================

        public async Task<ResourceDependencyResponse> GetResourceDependencies(string user, int accountId, string resourceType, string resourceId)
        {
            var allResources = await LoadAllResourcesFromDb(user, accountId);
            var edges = BuildEdgesFromDb(allResources);

            var resource = allResources.FirstOrDefault(r => r.ResourceId == resourceId && r.ResourceType == resourceType)
                ?? throw new KeyNotFoundException($"{resourceType} '{resourceId}' not found.");

            var relatedEdges = edges.Where(e => e.SourceId == resourceId || e.TargetId == resourceId).ToList();

            var dependsOn = relatedEdges
                .Where(e => e.TargetId == resourceId)
                .Select(e => allResources.FirstOrDefault(r => r.ResourceId == e.SourceId))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();

            var dependedBy = relatedEdges
                .Where(e => e.SourceId == resourceId)
                .Select(e => allResources.FirstOrDefault(r => r.ResourceId == e.TargetId))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();

            return new ResourceDependencyResponse
            {
                Resource = resource,
                DependsOn = dependsOn,
                DependedBy = dependedBy,
                Edges = relatedEdges
            };
        }

        // ==================================================================
        //  4. DELETION BLOCKERS (what prevents you from deleting a resource)
        // ==================================================================

        public async Task<DeletionBlockersResponse> GetDeletionBlockers(string user, int accountId, string resourceType, string resourceId)
        {
            var allResources = await LoadAllResourcesFromDb(user, accountId);
            var edges = BuildEdgesFromDb(allResources);

            var resource = allResources.FirstOrDefault(r => r.ResourceId == resourceId && r.ResourceType == resourceType)
                ?? throw new KeyNotFoundException($"{resourceType} '{resourceId}' not found.");

            // Find all descendants recursively
            var blockers = new List<ResourceSummary>();
            var deletionOrder = new List<ResourceSummary>();
            var messages = new List<string>();

            CollectDescendants(resourceId, edges, allResources, blockers, new HashSet<string>());

            // Build deletion order (leaves first, then parents)
            BuildDeletionOrder(resourceId, edges, allResources, deletionOrder, new HashSet<string>());

            // Generate human-readable messages
            foreach (var blocker in blockers)
            {
                var edge = edges.FirstOrDefault(e =>
                    (e.SourceId == resourceId && e.TargetId == blocker.ResourceId) ||
                    (e.TargetId == resourceId && e.SourceId == blocker.ResourceId));

                var relationship = edge?.Relationship ?? "linked";
                messages.Add($"{blocker.ResourceType} '{blocker.Name}' ({blocker.ResourceId}) — {relationship}");

                // Special messages for known error scenarios
                if (blocker.ResourceType == "ElasticIP" && resourceType == "InternetGateway")
                {
                    messages.Add($"  ⚠ Unmap/disassociate Elastic IP {blocker.ResourceId} before detaching the Internet Gateway.");
                }
                if (blocker.ResourceType == "NetworkInterface" && blocker.State == "in-use")
                {
                    messages.Add($"  ⚠ Network Interface {blocker.ResourceId} is in-use. Delete or detach the resource using it first.");
                }
            }

            if (resourceType == "VPC" || resourceType == "InternetGateway")
            {
                // Check for mapped public addresses that block IGW detach
                var eips = blockers.Where(b => b.ResourceType == "ElasticIP").ToList();
                if (eips.Count > 0)
                {
                    messages.Insert(0, $"⚠ This {resourceType} has {eips.Count} mapped public address(es). Unmap them before detaching/deleting.");
                }
            }

            return new DeletionBlockersResponse
            {
                Resource = resource,
                CanDelete = blockers.Count == 0,
                Blockers = blockers,
                DeletionOrder = deletionOrder,
                Messages = messages
            };
        }

        // ==================================================================
        //  5. MAPPED PUBLIC ADDRESSES (live cloud check for IGW errors)
        // ==================================================================

        public async Task<List<ResourceSummary>> GetMappedPublicAddresses(string user, int accountId, string vpcId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = MappedProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return await provider.FetchMappedPublicAddresses(account, vpcId);
        }

        // ==================================================================
        //  6. NETWORK INTERFACES (live cloud check)
        // ==================================================================

        public async Task<List<ResourceSummary>> GetNetworkInterfaces(string user, int accountId, string vpcId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = MappedProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            return await provider.FetchNetworkInterfaces(account, vpcId);
        }

        // ==================================================================
        //  7. SYNC (live cloud graph refresh)
        // ==================================================================

        public async Task<SyncMappedResult> SyncResourceGraph(string user, int accountId)
        {
            // Sync rebuilds the graph by querying the DB (which should be synced by individual services)
            // and also fetches live edges from cloud for the most accurate picture
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = MappedProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            // Get live edges from cloud
            var liveEdges = await provider.FetchAllResourceEdges(account);

            // Also get DB-based graph
            var allResources = await LoadAllResourcesFromDb(user, accountId);
            var dbEdges = BuildEdgesFromDb(allResources);

            // Merge: live edges take precedence, add DB edges not found in live
            var mergedEdges = new List<ResourceEdge>(liveEdges);
            foreach (var dbEdge in dbEdges)
            {
                var exists = mergedEdges.Any(e =>
                    e.SourceType == dbEdge.SourceType && e.SourceId == dbEdge.SourceId &&
                    e.TargetType == dbEdge.TargetType && e.TargetId == dbEdge.TargetId);
                if (!exists)
                    mergedEdges.Add(dbEdge);
            }

            // Build resource summaries from live edges
            var liveResourceKeys = new HashSet<string>();
            var liveResources = new List<ResourceSummary>();

            foreach (var edge in mergedEdges)
            {
                var sourceKey = $"{edge.SourceType}:{edge.SourceId}";
                if (liveResourceKeys.Add(sourceKey))
                {
                    liveResources.Add(new ResourceSummary
                    {
                        ResourceType = edge.SourceType, ResourceId = edge.SourceId,
                        Name = edge.SourceName, Provider = account.Provider!
                    });
                }
                var targetKey = $"{edge.TargetType}:{edge.TargetId}";
                if (liveResourceKeys.Add(targetKey))
                {
                    liveResources.Add(new ResourceSummary
                    {
                        ResourceType = edge.TargetType, ResourceId = edge.TargetId,
                        Name = edge.TargetName, Provider = account.Provider!
                    });
                }
            }

            var graph = BuildTreeFromEdges(liveResources, mergedEdges);

            var counts = liveResources.GroupBy(r => r.ResourceType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new SyncMappedResult
            {
                TotalResources = liveResources.Count,
                ResourceCounts = counts,
                TotalEdges = mergedEdges.Count,
                Graph = new ResourceGraphResponse
                {
                    Graph = graph,
                    Edges = mergedEdges,
                    Summary = counts,
                    TotalResources = liveResources.Count
                }
            };
        }

        // ==================================================================
        //  8. VPC RESOURCES FROM CLOUD (live)
        // ==================================================================

        public async Task<VpcResourceTreeResponse> GetLiveVpcResources(string user, int accountId, string vpcId)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId)
                ?? throw new InvalidOperationException("Cloud account not found.");

            var provider = MappedProviderFactory.Get(account.Provider
                ?? throw new InvalidOperationException("Cloud provider is not set."));

            var resources = await provider.FetchVpcResources(account, vpcId);

            var edges = BuildEdgesFromResources(resources);
            var vpcNode = BuildVpcTree(vpcId, vpcId, resources, edges);
            var hasIgw = resources.Any(r => r.ResourceType == "InternetGateway");

            var summary = resources.GroupBy(r => r.ResourceType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new VpcResourceTreeResponse
            {
                VpcTree = vpcNode,
                AllResources = resources,
                Edges = edges,
                Summary = summary,
                HasInternetGateway = hasIgw,
                TotalResources = resources.Count
            };
        }

        // ==================================================================
        //  INTERNAL: Load all resources from DB
        // ==================================================================

        private async Task<List<ResourceSummary>> LoadAllResourcesFromDb(string user, int accountId)
        {
            var resources = new List<ResourceSummary>();

            // VPCs
            var vpcs = await _db.VpcRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(vpcs.Select(v => new ResourceSummary
            {
                ResourceType = "VPC", ResourceId = v.VpcId, Name = v.Name, State = v.State,
                DbId = v.Id, Provider = v.Provider
            }));

            // Subnets
            var subnets = await _db.SubnetRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(subnets.Select(s => new ResourceSummary
            {
                ResourceType = "Subnet", ResourceId = s.SubnetId, Name = s.Name, State = s.State,
                DbId = s.Id, Provider = s.Provider, ParentResourceId = s.VpcId, ParentResourceType = "VPC"
            }));

            // Security Groups
            var sgs = await _db.SecurityGroupRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(sgs.Select(sg => new ResourceSummary
            {
                ResourceType = "SecurityGroup", ResourceId = sg.SecurityGroupId, Name = sg.GroupName,
                State = "active", DbId = sg.Id, Provider = sg.Provider,
                ParentResourceId = sg.VpcId, ParentResourceType = "VPC"
            }));

            // Internet Gateways
            var igws = await _db.InternetGatewayRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(igws.Select(igw => new ResourceSummary
            {
                ResourceType = "InternetGateway", ResourceId = igw.InternetGatewayId, Name = igw.Name,
                State = igw.State, DbId = igw.Id, Provider = igw.Provider,
                ParentResourceId = igw.AttachedVpcId, ParentResourceType = "VPC"
            }));

            // EC2 Instances
            var ec2s = await _db.Ec2InstanceRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(ec2s.Select(e => new ResourceSummary
            {
                ResourceType = "EC2", ResourceId = e.InstanceId, Name = e.Name, State = e.State,
                DbId = e.Id, Provider = e.Provider,
                ParentResourceId = !string.IsNullOrEmpty(e.SubnetId) ? e.SubnetId : e.VpcId,
                ParentResourceType = !string.IsNullOrEmpty(e.SubnetId) ? "Subnet" : "VPC"
            }));

            // ECS Clusters
            var clusters = await _db.ClusterRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(clusters.Select(c => new ResourceSummary
            {
                ResourceType = "ECSCluster", ResourceId = c.ClusterArn ?? c.Name, Name = c.Name,
                State = c.Status, DbId = c.Id, Provider = c.Provider
            }));

            // ECS Services
            var ecsServices = await _db.EcsServiceRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(ecsServices.Select(s => new ResourceSummary
            {
                ResourceType = "ECSService", ResourceId = s.ServiceArn ?? s.ServiceName, Name = s.ServiceName,
                State = s.Status, DbId = s.Id, Provider = s.Provider,
                ParentResourceId = s.ClusterArn ?? s.ClusterName, ParentResourceType = "ECSCluster"
            }));

            // ECS Tasks
            var ecsTasks = await _db.EcsTaskRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(ecsTasks.Select(t => new ResourceSummary
            {
                ResourceType = "ECSTask", ResourceId = t.TaskArn, Name = t.TaskArn,
                State = t.LastStatus, DbId = t.Id, Provider = t.Provider,
                ParentResourceId = t.ClusterArn ?? t.ClusterName, ParentResourceType = "ECSCluster"
            }));

            // ECS Task Definitions
            var ecsTds = await _db.EcsTaskDefinitionRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(ecsTds.Select(td => new ResourceSummary
            {
                ResourceType = "ECSTaskDefinition", ResourceId = td.TaskDefinitionArn ?? $"{td.Family}:{td.Revision}",
                Name = $"{td.Family}:{td.Revision}", State = td.Status, DbId = td.Id, Provider = td.Provider
            }));

            // ECR Repositories
            var ecrRepos = await _db.EcrRepositoryRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(ecrRepos.Select(r => new ResourceSummary
            {
                ResourceType = "ECRRepository", ResourceId = r.RepositoryArn ?? r.RepositoryName,
                Name = r.RepositoryName, State = "active", DbId = r.Id, Provider = r.Provider
            }));

            // ECR Images
            var ecrImages = await _db.EcrImageRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(ecrImages.Select(img => new ResourceSummary
            {
                ResourceType = "ECRImage", ResourceId = img.ImageDigest ?? $"{img.RepositoryName}:{img.ImageTag}",
                Name = $"{img.RepositoryName}:{img.ImageTag ?? "untagged"}", State = img.ScanStatus ?? "unknown",
                DbId = img.Id, Provider = img.Provider,
                ParentResourceId = img.RepositoryName, ParentResourceType = "ECRRepository"
            }));

            // S3 Buckets
            var buckets = await _db.BucketRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(buckets.Select(b => new ResourceSummary
            {
                ResourceType = "S3Bucket", ResourceId = b.Name, Name = b.Name,
                State = b.Status, DbId = b.Id, Provider = b.Provider
            }));

            // Cloud Files (S3 objects)
            var files = await _db.CloudFiles.Where(x => x.CloudAccountId == accountId && x.UploadedBy == user).ToListAsync();
            resources.AddRange(files.Select(f => new ResourceSummary
            {
                ResourceType = "S3Object", ResourceId = $"{f.BucketName}/{f.Folder}{f.FileName}",
                Name = f.FileName, State = "available", DbId = f.Id, Provider = f.Provider,
                ParentResourceId = f.BucketName, ParentResourceType = "S3Bucket"
            }));

            // CloudWatch Log Groups
            var logGroups = await _db.LogGroupRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(logGroups.Select(lg => new ResourceSummary
            {
                ResourceType = "LogGroup", ResourceId = lg.Arn ?? lg.LogGroupName, Name = lg.LogGroupName,
                State = "active", DbId = lg.Id, Provider = lg.Provider
            }));

            // CloudWatch Log Streams
            var logStreams = await _db.LogStreamRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(logStreams.Select(ls => new ResourceSummary
            {
                ResourceType = "LogStream", ResourceId = ls.Arn ?? ls.LogStreamName, Name = ls.LogStreamName,
                State = "active", DbId = ls.Id, Provider = ls.Provider,
                ParentResourceId = ls.LogGroupName, ParentResourceType = "LogGroup"
            }));

            // Key Pairs
            var keyPairs = await _db.KeyPairRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(keyPairs.Select(kp => new ResourceSummary
            {
                ResourceType = "KeyPair", ResourceId = kp.KeyPairId, Name = kp.KeyName,
                State = "active", DbId = kp.Id, Provider = kp.Provider
            }));

            // EC2 Instance Connect Endpoints
            var eicEndpoints = await _db.Ec2InstanceConnectEndpointRecords.Where(x => x.CloudAccountId == accountId && x.CreatedBy == user).ToListAsync();
            resources.AddRange(eicEndpoints.Select(ep => new ResourceSummary
            {
                ResourceType = "EICEndpoint", ResourceId = ep.EndpointId, Name = ep.EndpointId,
                State = ep.State, DbId = ep.Id, Provider = ep.Provider,
                ParentResourceId = ep.VpcId, ParentResourceType = "VPC"
            }));

            // Build extra edges: EC2 → SecurityGroup (from JSON field)
            foreach (var ec2 in ec2s.Where(e => !string.IsNullOrEmpty(e.SecurityGroupsJson)))
            {
                try
                {
                    var sgIds = JsonSerializer.Deserialize<List<string>>(ec2.SecurityGroupsJson!);
                    if (sgIds != null)
                    {
                        foreach (var sgId in sgIds)
                        {
                            // Add a virtual SG→EC2 reference for the graph
                            var sgResource = resources.FirstOrDefault(r => r.ResourceType == "SecurityGroup" && r.ResourceId == sgId);
                            if (sgResource != null)
                            {
                                // This relationship is tracked via edges, not parent
                            }
                        }
                    }
                }
                catch { /* ignore malformed JSON */ }
            }

            // Build extra edges: KeyPair → EC2
            foreach (var ec2 in ec2s.Where(e => !string.IsNullOrEmpty(e.KeyName)))
            {
                var kp = keyPairs.FirstOrDefault(k => k.KeyName == ec2.KeyName);
                if (kp != null)
                {
                    // This is tracked via edges
                }
            }

            return resources;
        }

        // ==================================================================
        //  INTERNAL: Build edges from DB resources
        // ==================================================================

        private List<ResourceEdge> BuildEdgesFromDb(List<ResourceSummary> resources)
        {
            var edges = new List<ResourceEdge>();

            // Parent → Child edges (from ParentResourceId)
            foreach (var resource in resources.Where(r => !string.IsNullOrEmpty(r.ParentResourceId)))
            {
                var parent = resources.FirstOrDefault(p => p.ResourceId == resource.ParentResourceId);
                if (parent != null)
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = parent.ResourceType, SourceId = parent.ResourceId, SourceName = parent.Name,
                        TargetType = resource.ResourceType, TargetId = resource.ResourceId, TargetName = resource.Name,
                        Relationship = GetRelationship(parent.ResourceType, resource.ResourceType)
                    });
                }
            }

            // EC2 → SecurityGroup edges (from JSON)
            var ec2Resources = resources.Where(r => r.ResourceType == "EC2").ToList();
            foreach (var ec2Res in ec2Resources)
            {
                var ec2Record = _db.Ec2InstanceRecords.Local.FirstOrDefault(e => e.InstanceId == ec2Res.ResourceId);
                if (ec2Record?.SecurityGroupsJson != null)
                {
                    try
                    {
                        var sgIds = JsonSerializer.Deserialize<List<string>>(ec2Record.SecurityGroupsJson);
                        if (sgIds != null)
                        {
                            foreach (var sgId in sgIds)
                            {
                                var sg = resources.FirstOrDefault(r => r.ResourceType == "SecurityGroup" && r.ResourceId == sgId);
                                if (sg != null)
                                {
                                    edges.Add(new ResourceEdge
                                    {
                                        SourceType = "SecurityGroup", SourceId = sg.ResourceId, SourceName = sg.Name,
                                        TargetType = "EC2", TargetId = ec2Res.ResourceId, TargetName = ec2Res.Name,
                                        Relationship = "protects"
                                    });
                                }
                            }
                        }
                    }
                    catch { /* ignore malformed JSON */ }
                }
            }

            // KeyPair → EC2 edges
            foreach (var ec2Res in ec2Resources)
            {
                var ec2Record = _db.Ec2InstanceRecords.Local.FirstOrDefault(e => e.InstanceId == ec2Res.ResourceId);
                if (!string.IsNullOrEmpty(ec2Record?.KeyName))
                {
                    var kp = resources.FirstOrDefault(r => r.ResourceType == "KeyPair" && r.Name == ec2Record.KeyName);
                    if (kp != null)
                    {
                        edges.Add(new ResourceEdge
                        {
                            SourceType = "KeyPair", SourceId = kp.ResourceId, SourceName = kp.Name,
                            TargetType = "EC2", TargetId = ec2Res.ResourceId, TargetName = ec2Res.Name,
                            Relationship = "used-by"
                        });
                    }
                }
            }

            // EC2 → VPC edge (if EC2 is parented under Subnet, also add VPC relationship)
            foreach (var ec2Res in ec2Resources)
            {
                var ec2Record = _db.Ec2InstanceRecords.Local.FirstOrDefault(e => e.InstanceId == ec2Res.ResourceId);
                if (ec2Record != null && !string.IsNullOrEmpty(ec2Record.VpcId) && !string.IsNullOrEmpty(ec2Record.SubnetId))
                {
                    var vpc = resources.FirstOrDefault(r => r.ResourceType == "VPC" && r.ResourceId == ec2Record.VpcId);
                    if (vpc != null)
                    {
                        // Only add if not already present
                        if (!edges.Any(e => e.SourceId == vpc.ResourceId && e.TargetId == ec2Res.ResourceId && e.SourceType == "VPC"))
                        {
                            edges.Add(new ResourceEdge
                            {
                                SourceType = "VPC", SourceId = vpc.ResourceId, SourceName = vpc.Name,
                                TargetType = "EC2", TargetId = ec2Res.ResourceId, TargetName = ec2Res.Name,
                                Relationship = "contains"
                            });
                        }
                    }
                }
            }

            // EIC Endpoint → SecurityGroup edges
            var eicEndpoints = resources.Where(r => r.ResourceType == "EICEndpoint").ToList();
            foreach (var eicRes in eicEndpoints)
            {
                var eicRecord = _db.Ec2InstanceConnectEndpointRecords.Local.FirstOrDefault(e => e.EndpointId == eicRes.ResourceId);
                if (eicRecord?.SecurityGroupIdsJson != null)
                {
                    try
                    {
                        var sgIds = JsonSerializer.Deserialize<List<string>>(eicRecord.SecurityGroupIdsJson);
                        if (sgIds != null)
                        {
                            foreach (var sgId in sgIds)
                            {
                                var sg = resources.FirstOrDefault(r => r.ResourceType == "SecurityGroup" && r.ResourceId == sgId);
                                if (sg != null)
                                {
                                    edges.Add(new ResourceEdge
                                    {
                                        SourceType = "SecurityGroup", SourceId = sg.ResourceId, SourceName = sg.Name,
                                        TargetType = "EICEndpoint", TargetId = eicRes.ResourceId, TargetName = eicRes.Name,
                                        Relationship = "protects"
                                    });
                                }
                            }
                        }
                    }
                    catch { /* ignore malformed JSON */ }
                }
            }

            return edges;
        }

        private static List<ResourceEdge> BuildEdgesFromResources(List<ResourceSummary> resources)
        {
            var edges = new List<ResourceEdge>();

            foreach (var resource in resources.Where(r => !string.IsNullOrEmpty(r.ParentResourceId)))
            {
                var parent = resources.FirstOrDefault(p => p.ResourceId == resource.ParentResourceId);
                if (parent != null)
                {
                    edges.Add(new ResourceEdge
                    {
                        SourceType = parent.ResourceType, SourceId = parent.ResourceId, SourceName = parent.Name,
                        TargetType = resource.ResourceType, TargetId = resource.ResourceId, TargetName = resource.Name,
                        Relationship = GetRelationship(parent.ResourceType, resource.ResourceType)
                    });
                }
            }

            return edges;
        }

        // ==================================================================
        //  INTERNAL: Build tree from flat edges
        // ==================================================================

        private static List<ResourceNode> BuildTreeFromEdges(List<ResourceSummary> resources, List<ResourceEdge> edges)
        {
            var nodeMap = new Dictionary<string, ResourceNode>();
            foreach (var r in resources)
            {
                var key = $"{r.ResourceType}:{r.ResourceId}";
                if (!nodeMap.ContainsKey(key))
                {
                    nodeMap[key] = new ResourceNode
                    {
                        ResourceType = r.ResourceType, ResourceId = r.ResourceId,
                        Name = r.Name, State = r.State, DbId = r.DbId, Provider = r.Provider
                    };
                }
            }

            var childIds = new HashSet<string>();

            foreach (var edge in edges)
            {
                var parentKey = $"{edge.SourceType}:{edge.SourceId}";
                var childKey = $"{edge.TargetType}:{edge.TargetId}";

                if (nodeMap.TryGetValue(parentKey, out var parentNode) && nodeMap.TryGetValue(childKey, out var childNode))
                {
                    if (edge.Relationship is "contains" or "attached-to" or "main-route-table")
                    {
                        if (!parentNode.Children.Any(c => c.ResourceId == childNode.ResourceId && c.ResourceType == childNode.ResourceType))
                        {
                            parentNode.Children.Add(childNode);
                            childIds.Add(childKey);
                        }
                    }
                }
            }

            // Calculate total descendants
            foreach (var node in nodeMap.Values)
            {
                node.TotalDescendants = CountDescendants(node);
            }

            // Root nodes are those not appearing as children
            return nodeMap.Values.Where(n => !childIds.Contains($"{n.ResourceType}:{n.ResourceId}")).ToList();
        }

        private static ResourceNode BuildVpcTree(string vpcId, string vpcName, List<ResourceSummary> resources, List<ResourceEdge> edges)
        {
            var vpcNode = new ResourceNode
            {
                ResourceType = "VPC", ResourceId = vpcId, Name = vpcName,
                State = "available", Provider = "AWS"
            };

            // Group children by type
            var childMap = new Dictionary<string, List<ResourceNode>>();
            foreach (var res in resources.Where(r => r.ResourceType != "VPC"))
            {
                if (!childMap.ContainsKey(res.ResourceType))
                    childMap[res.ResourceType] = new List<ResourceNode>();

                childMap[res.ResourceType].Add(new ResourceNode
                {
                    ResourceType = res.ResourceType, ResourceId = res.ResourceId,
                    Name = res.Name, State = res.State, DbId = res.DbId, Provider = res.Provider
                });
            }

            // Nest EC2 under Subnets
            var subnets = childMap.GetValueOrDefault("Subnet") ?? new();
            var ec2s = childMap.GetValueOrDefault("EC2") ?? new();

            foreach (var subnet in subnets)
            {
                var subnetEc2s = ec2s.Where(e =>
                    resources.Any(r => r.ResourceId == e.ResourceId && r.ParentResourceId == subnet.ResourceId)
                ).ToList();

                subnet.Children.AddRange(subnetEc2s);

                // Also nest NetworkInterfaces under Subnets
                var subnetEnis = (childMap.GetValueOrDefault("NetworkInterface") ?? new())
                    .Where(eni => resources.Any(r => r.ResourceId == eni.ResourceId && r.ParentResourceId == subnet.ResourceId))
                    .ToList();
                subnet.Children.AddRange(subnetEnis);

                subnet.TotalDescendants = CountDescendants(subnet);
            }

            // Add all type groups to VPC
            foreach (var (type, nodes) in childMap)
            {
                if (type == "EC2" || type == "NetworkInterface") continue; // already nested under subnets
                vpcNode.Children.AddRange(nodes);
            }

            vpcNode.TotalDescendants = CountDescendants(vpcNode);
            return vpcNode;
        }

        // ==================================================================
        //  INTERNAL: Deletion helpers
        // ==================================================================

        private static void CollectDescendants(string resourceId, List<ResourceEdge> edges,
            List<ResourceSummary> resources, List<ResourceSummary> result, HashSet<string> visited)
        {
            if (!visited.Add(resourceId)) return;

            var childEdges = edges.Where(e => e.SourceId == resourceId).ToList();
            foreach (var edge in childEdges)
            {
                var child = resources.FirstOrDefault(r => r.ResourceId == edge.TargetId);
                if (child != null && !result.Any(r => r.ResourceId == child.ResourceId))
                {
                    result.Add(child);
                    CollectDescendants(child.ResourceId, edges, resources, result, visited);
                }
            }
        }

        private static void BuildDeletionOrder(string resourceId, List<ResourceEdge> edges,
            List<ResourceSummary> resources, List<ResourceSummary> result, HashSet<string> visited)
        {
            if (!visited.Add(resourceId)) return;

            var childEdges = edges.Where(e => e.SourceId == resourceId).ToList();
            foreach (var edge in childEdges)
            {
                BuildDeletionOrder(edge.TargetId, edges, resources, result, visited);
            }

            var resource = resources.FirstOrDefault(r => r.ResourceId == resourceId);
            if (resource != null && !result.Any(r => r.ResourceId == resource.ResourceId))
            {
                result.Add(resource);
            }
        }

        // ==================================================================
        //  INTERNAL: Helpers
        // ==================================================================

        private static int CountDescendants(ResourceNode node)
        {
            int count = node.Children.Count;
            foreach (var child in node.Children)
            {
                count += CountDescendants(child);
            }
            return count;
        }

        private static string GetRelationship(string parentType, string childType) => (parentType, childType) switch
        {
            ("VPC", "Subnet") => "contains",
            ("VPC", "SecurityGroup") => "contains",
            ("VPC", "InternetGateway") => "attached-to",
            ("VPC", "EC2") => "contains",
            ("VPC", "EICEndpoint") => "contains",
            ("VPC", "RouteTable") => "contains",
            ("VPC", "NATGateway") => "contains",
            ("Subnet", "EC2") => "contains",
            ("Subnet", "EICEndpoint") => "contains",
            ("Subnet", "NATGateway") => "contains",
            ("Subnet", "NetworkInterface") => "contains",
            ("SecurityGroup", "EC2") => "protects",
            ("SecurityGroup", "EICEndpoint") => "protects",
            ("SecurityGroup", "NetworkInterface") => "protects",
            ("KeyPair", "EC2") => "used-by",
            ("ECSCluster", "ECSService") => "contains",
            ("ECSCluster", "ECSTask") => "contains",
            ("ECRRepository", "ECRImage") => "contains",
            ("S3Bucket", "S3Object") => "contains",
            ("LogGroup", "LogStream") => "contains",
            ("RouteTable", "Subnet") => "associated-with",
            ("RouteTable", "InternetGateway") => "routes-to",
            ("RouteTable", "NATGateway") => "routes-to",
            ("NATGateway", "ElasticIP") => "uses",
            ("EC2", "ElasticIP") => "has-public-ip",
            ("NetworkInterface", "ElasticIP") => "mapped-public-address",
            _ => "related-to"
        };
    }
}
