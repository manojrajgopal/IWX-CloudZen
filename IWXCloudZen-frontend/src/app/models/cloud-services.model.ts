export interface Cluster {
  id: number;
  name: string;
  clusterArn: string | null;
  status: string;
  provider: string;
  cloudAccountId: number;
  containerInsightsEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface S3Bucket {
  id: number;
  name: string;
  region: string;
  status: string;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateBucketRequest {
  bucketName: string;
}

export interface CreateClusterRequest {
  clusterName: string;
}

export interface UpdateClusterRequest {
  enableContainerInsights?: boolean;
}

export interface DeleteClusterResponse {
  clusterArn: string;
  status: string;
}

export interface Vpc {
  id: number;
  name: string;
  vpcId: string;
  cidrBlock: string;
  state: string;
  isDefault: boolean;
  enableDnsSupport: boolean;
  enableDnsHostnames: boolean;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateVpcRequest {
  vpcName: string;
  cidrBlock: string;
  enableDnsSupport: boolean;
  enableDnsHostnames: boolean;
}

export interface UpdateVpcRequest {
  enableDnsSupport?: boolean;
  enableDnsHostnames?: boolean;
  amazonProvidedIpv6CidrBlock?: boolean;
}

export interface DeleteVpcResponse {
  vpcId: string;
  status: string;
}

export interface VpcSyncResponse {
  added: number;
  updated: number;
  removed: number;
  vpcs: Vpc[];
}

export interface EcrRepository {
  id: number;
  repositoryName: string;
  repositoryUri: string;
  registryId: string;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface EcsService {
  id: number;
  serviceName: string;
  serviceArn: string;
  clusterName: string;
  clusterArn: string;
  taskDefinition: string;
  desiredCount: number;
  runningCount: number;
  pendingCount: number;
  status: string;
  launchType: string;
  schedulingStrategy: string;
  networkConfigurationJson: string;
  provider: string;
  cloudAccountId: number;
  serviceCreatedAt: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface EcsTaskDefinition {
  id: number;
  family: string;
  taskDefinitionArn: string;
  revision: number;
  status: string;
  cpu: string;
  memory: string;
  networkMode: string;
  executionRoleArn: string;
  taskRoleArn: string | null;
  requiresCompatibilities: string;
  osFamily: string;
  containerCount: number;
  containerDefinitionsJson: string;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface EcsTask {
  id: number;
  taskArn: string;
  clusterName: string;
  clusterArn: string;
  taskDefinitionArn: string;
  group: string;
  lastStatus: string;
  desiredStatus: string;
  cpu: string;
  memory: string;
  launchType: string;
  connectivity: string | null;
  stopCode: string | null;
  stoppedReason: string | null;
  startedAt: string | null;
  stoppedAt: string | null;
  pullStartedAt: string | null;
  pullStoppedAt: string | null;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface EcsTaskDefinitionsResponse {
  totalCount: number;
  familyFilter: string | null;
  taskDefinitions: EcsTaskDefinition[];
}

export interface EcsTasksResponse {
  clusterName: string;
  totalCount: number;
  tasks: EcsTask[];
}

export interface EcsSyncTaskDefinitionsResult {
  added: number;
  updated: number;
  removed: number;
  taskDefinitions: EcsTaskDefinition[];
}

export interface EcsSyncClusterServicesResult {
  clusterName: string;
  added: number;
  updated: number;
  removed: number;
  services: EcsService[];
}

export interface EcsSyncClusterTasksResult {
  clusterName: string;
  added: number;
  updated: number;
  removed: number;
  tasks: any[];
}

export interface EcsSyncResponse {
  taskDefinitions: EcsSyncTaskDefinitionsResult;
  servicesByCluster: EcsSyncClusterServicesResult[];
  tasksByCluster: EcsSyncClusterTasksResult[];
  clustersSynced: string[];
  syncedAt: string;
}

export interface Subnet {
  id: number;
  subnetId: string;
  name: string;
  vpcId: string;
  cidrBlock: string;
  ipv6CidrBlock: string | null;
  availabilityZone: string;
  availabilityZoneId: string;
  state: string;
  availableIpAddressCount: number;
  isDefault: boolean;
  mapPublicIpOnLaunch: boolean;
  assignIpv6AddressOnCreation: boolean;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface SecurityGroupRule {
  ruleId: string;
  protocol: string;
  fromPort: number;
  toPort: number;
  ipv4Ranges: string[];
  ipv6Ranges: string[];
  referencedGroupIds: string[];
  description: string | null;
}

export interface SecurityGroup {
  id: number;
  securityGroupId: string;
  groupName: string;
  description: string;
  vpcId: string;
  ownerId: string;
  inboundRules: SecurityGroupRule[];
  outboundRules: SecurityGroupRule[];
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface LogGroup {
  id: number;
  logGroupName: string;
  arn: string;
  retentionInDays: number | null;
  storedBytes: number;
  metricFilterCount: string;
  kmsKeyId: string | null;
  dataProtectionStatus: string | null;
  logGroupClass: string;
  creationTimeUtc: string;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface Ec2Instance {
  id: number;
  instanceId: string;
  name: string;
  instanceType: string;
  state: string;
  publicIpAddress: string | null;
  privateIpAddress: string;
  vpcId: string;
  subnetId: string;
  imageId: string;
  keyName: string;
  architecture: string;
  platform: string;
  monitoring: string;
  ebsOptimized: boolean;
  securityGroups: { groupId: string; groupName: string }[];
  tags: { [key: string]: string };
  launchTime: string;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

// Cloud Storage Sync Response
export interface CloudFileResponse {
  id: number;
  fileName: string;
  fileUrl: string;
  bucketName: string;
  folder: string;
  size: number;
  contentType: string;
  provider: string;
  cloudAccountId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface BucketFileSyncResult {
  bucket: S3Bucket;
  filesAdded: number;
  filesUpdated: number;
  filesRemoved: number;
  files: CloudFileResponse[];
}

export interface FullSyncResult {
  bucketsAdded: number;
  bucketsUpdated: number;
  bucketsRemoved: number;
  buckets: BucketFileSyncResult[];
}

export interface FileListResponse {
  files: CloudFileResponse[];
}

export interface BucketFileSyncResponse {
  added: number;
  updated: number;
  removed: number;
  files: CloudFileResponse[];
}

// API response wrappers
export interface ClustersResponse { clusters: Cluster[]; }
export interface ClusterSyncResponse {
  added: number;
  updated: number;
  removed: number;
  clusters: Cluster[];
}
export interface BucketsResponse { buckets: S3Bucket[]; }
export interface VpcsResponse { vpcs: Vpc[]; }
export interface VpcSyncResponseWrapper { added: number; updated: number; removed: number; vpcs: Vpc[]; }
export interface EcrRepositoriesResponse { totalRepositories: number; repositories: EcrRepository[]; }
export interface EcsServicesResponse { clusterName: string; totalCount: number; services: EcsService[]; }
export interface SubnetsResponse { totalCount: number; vpcIdFilter: string | null; subnets: Subnet[]; }
export interface SubnetSyncResponse { added: number; updated: number; removed: number; subnets: Subnet[]; }
export interface CreateSubnetRequest { name: string; vpcId: string; cidrBlock: string; availabilityZone: string; mapPublicIpOnLaunch: boolean; ipv6CidrBlock: string | null; }
export interface SecurityGroupsResponse { totalCount: number; vpcIdFilter: string | null; securityGroups: SecurityGroup[]; }
export interface SecurityGroupSyncResponse { added: number; updated: number; removed: number; securityGroups: SecurityGroup[]; }
export interface CreateSecurityGroupRuleRequest { protocol: string; fromPort: number; toPort: number; ipv4Ranges: string[]; ipv6Ranges: string[]; referencedGroupIds: string[]; description: string | null; }
export interface CreateSecurityGroupRequest { groupName: string; description: string; vpcId: string; inboundRules: CreateSecurityGroupRuleRequest[]; outboundRules: CreateSecurityGroupRuleRequest[]; }
export interface LogGroupsResponse { logGroups: LogGroup[]; }
export interface Ec2InstancesResponse { instances: Ec2Instance[]; }

// ECS Create requests
export interface CreateEcsServiceNetworkConfig {
  subnets: string[];
  securityGroups: string[];
  assignPublicIp: boolean;
}

export interface CreateEcsServiceRequest {
  serviceName: string;
  clusterName: string;
  taskDefinition: string;
  desiredCount: number;
  launchType: string;
  schedulingStrategy: string;
  networkConfiguration: CreateEcsServiceNetworkConfig;
}

export interface ContainerPortMapping {
  containerPort: number;
  hostPort: number;
  protocol: string;
}

export interface ContainerEnvironment {
  name: string;
  value: string;
}

export interface ContainerLogConfiguration {
  logDriver: string;
  options: Record<string, string>;
}

export interface CreateContainerDefinition {
  name: string;
  image: string;
  cpu: number;
  memory: number;
  memoryReservation: number | null;
  essential: boolean;
  portMappings: ContainerPortMapping[];
  environment: ContainerEnvironment[];
  logConfiguration: ContainerLogConfiguration | null;
}

export interface CreateTaskDefinitionRequest {
  family: string;
  cpu: string;
  memory: string;
  networkMode: string;
  executionRoleArn: string;
  taskRoleArn: string | null;
  requiresCompatibilities: string[];
  osFamily: string;
  containerDefinitions: CreateContainerDefinition[];
}
