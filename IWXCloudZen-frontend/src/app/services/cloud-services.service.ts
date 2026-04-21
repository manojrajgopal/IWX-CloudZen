import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClustersResponse,
  ClusterSyncResponse,
  BucketsResponse,
  VpcsResponse,
  VpcSyncResponse,
  EcrRepositoriesResponse,
  EcsServicesResponse,
  EcsService,
  EcsTaskDefinition,
  EcsTaskDefinitionsResponse,
  EcsTask,
  EcsTasksResponse,
  EcsSyncResponse,
  SubnetsResponse,
  SubnetSyncResponse,
  CreateSubnetRequest,
  UpdateSubnetRequest,
  DeleteSubnetResponse,
  Subnet,
  SecurityGroupsResponse,
  SecurityGroupSyncResponse,
  SecurityGroup,
  CreateSecurityGroupRequest,
  UpdateSecurityGroupRequest,
  DeleteSecurityGroupResponse,
  AddRulesRequest,
  RemoveRulesRequest,
  LogGroupsResponse,
  Ec2InstancesResponse,
  Ec2Instance,
  LaunchEc2InstanceRequest,
  UpdateEc2InstanceRequest,
  Ec2SyncResponse,
  KeyPairsResponse,
  KeyPair,
  CreateKeyPairRequest,
  UpdateKeyPairTagsRequest,
  KeyPairDeleteResponse,
  DownloadPrivateKeyResponse,
  KeyPairSyncResponse,
  FullSyncResult,
  FileListResponse,
  BucketFileSyncResponse,
  S3Bucket,
  Cluster,
  Vpc,
  CloudFileResponse,
  CreateBucketRequest,
  CreateClusterRequest,
  CreateVpcRequest,
  UpdateClusterRequest,
  DeleteClusterResponse,
  UpdateVpcRequest,
  DeleteVpcResponse,
  CreateEcsServiceRequest,
  UpdateEcsServiceRequest,
  CreateTaskDefinitionRequest,
  RunTaskRequest,
  StopTaskRequest,
  SyncEcsServicesResult,
  SyncEcsTasksResult,
  SyncTaskDefinitionsResult,
  CheckPermissionRequest,
  PermissionCheckResponse,
  PermissionSummaryResponse,
  InternetGateway,
  InternetGatewaysResponse,
  InternetGatewayVpcResponse,
  CreateInternetGatewayRequest,
  UpdateInternetGatewayRequest,
  InternetGatewayDeleteResponse,
  AttachInternetGatewayRequest,
  DetachInternetGatewayRequest,
  InternetGatewaySyncResponse,
  PoliciesResponse,
  AvailablePoliciesResponse,
  AttachPolicyRequest,
  PolicyActionResponse,
  SyncPoliciesResponse,
  ListPoliciesResponse
} from '../models/cloud-services.model';

@Injectable({
  providedIn: 'root'
})
export class CloudServicesService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getClusters(accountId: number): Observable<ClustersResponse> {
    return this.http.get<ClustersResponse>(
      `${this.apiUrl}/api/cloud/services/cluster/aws/list?accountId=${accountId}`
    );
  }

  createCluster(accountId: number, request: CreateClusterRequest): Observable<Cluster> {
    return this.http.post<Cluster>(
      `${this.apiUrl}/api/cloud/services/cluster/aws/create?accountId=${accountId}`,
      request
    );
  }

  syncClusters(accountId: number): Observable<ClusterSyncResponse> {
    return this.http.post<ClusterSyncResponse>(
      `${this.apiUrl}/api/cloud/services/cluster/aws/sync?accountId=${accountId}`,
      null
    );
  }

  updateCluster(clusterId: number, accountId: number, request: UpdateClusterRequest): Observable<Cluster> {
    return this.http.put<Cluster>(
      `${this.apiUrl}/api/cloud/services/cluster/aws/update/${clusterId}?accountId=${accountId}`,
      request
    );
  }

  deleteCluster(clusterId: number, accountId: number): Observable<DeleteClusterResponse> {
    return this.http.delete<DeleteClusterResponse>(
      `${this.apiUrl}/api/cloud/services/cluster/aws/delete/${clusterId}?accountId=${accountId}`
    );
  }

  getS3Buckets(accountId: number): Observable<BucketsResponse> {
    return this.http.get<BucketsResponse>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/buckets?accountId=${accountId}`
    );
  }

  getVpcs(accountId: number): Observable<VpcsResponse> {
    return this.http.get<VpcsResponse>(
      `${this.apiUrl}/api/cloud/services/vpc/aws/list?accountId=${accountId}`
    );
  }

  createVpc(accountId: number, request: CreateVpcRequest): Observable<Vpc> {
    return this.http.post<Vpc>(
      `${this.apiUrl}/api/cloud/services/vpc/aws/create?accountId=${accountId}`,
      request
    );
  }

  syncVpcs(accountId: number): Observable<VpcSyncResponse> {
    return this.http.post<VpcSyncResponse>(
      `${this.apiUrl}/api/cloud/services/vpc/aws/sync?accountId=${accountId}`,
      null
    );
  }

  updateVpc(vpcId: number, accountId: number, request: UpdateVpcRequest): Observable<Vpc> {
    return this.http.put<Vpc>(
      `${this.apiUrl}/api/cloud/services/vpc/aws/update/${vpcId}?accountId=${accountId}`,
      request
    );
  }

  deleteVpc(vpcId: number, accountId: number): Observable<DeleteVpcResponse> {
    return this.http.delete<DeleteVpcResponse>(
      `${this.apiUrl}/api/cloud/services/vpc/aws/delete/${vpcId}?accountId=${accountId}`
    );
  }

  getEcrRepositories(accountId: number): Observable<EcrRepositoriesResponse> {
    return this.http.get<EcrRepositoriesResponse>(
      `${this.apiUrl}/api/cloud/services/ecr/aws/repositories?accountId=${accountId}`
    );
  }

  getEcsServices(accountId: number): Observable<EcsServicesResponse> {
    return this.http.get<EcsServicesResponse>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/services?accountId=${accountId}`
    );
  }

  getEcsServiceById(serviceId: number, accountId: number): Observable<EcsService> {
    return this.http.get<EcsService>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/services/${serviceId}?accountId=${accountId}`
    );
  }

  createEcsService(accountId: number, request: CreateEcsServiceRequest): Observable<EcsService> {
    return this.http.post<EcsService>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/services?accountId=${accountId}`,
      request
    );
  }

  getEcsTaskDefinitions(accountId: number): Observable<EcsTaskDefinitionsResponse> {
    return this.http.get<EcsTaskDefinitionsResponse>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/task-definitions?accountId=${accountId}`
    );
  }

  getEcsTaskDefinitionById(id: number, accountId: number): Observable<EcsTaskDefinition> {
    return this.http.get<EcsTaskDefinition>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/task-definitions/${id}?accountId=${accountId}`
    );
  }

  createTaskDefinition(accountId: number, request: CreateTaskDefinitionRequest): Observable<EcsTaskDefinition> {
    return this.http.post<EcsTaskDefinition>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/task-definitions?accountId=${accountId}`,
      request
    );
  }

  getEcsTasks(accountId: number): Observable<EcsTasksResponse> {
    return this.http.get<EcsTasksResponse>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/tasks?accountId=${accountId}`
    );
  }

  getEcsTaskById(id: number, accountId: number): Observable<EcsTask> {
    return this.http.get<EcsTask>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/tasks/${id}?accountId=${accountId}`
    );
  }

  syncEcs(accountId: number): Observable<EcsSyncResponse> {
    return this.http.post<EcsSyncResponse>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/sync?accountId=${accountId}`,
      null
    );
  }

  getSubnets(accountId: number): Observable<SubnetsResponse> {
    return this.http.get<SubnetsResponse>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/list?accountId=${accountId}`
    );
  }

  syncSubnets(accountId: number): Observable<SubnetSyncResponse> {
    return this.http.post<SubnetSyncResponse>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/sync?accountId=${accountId}`,
      null
    );
  }

  createSubnet(accountId: number, request: CreateSubnetRequest): Observable<Subnet> {
    return this.http.post<Subnet>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/create?accountId=${accountId}`,
      request
    );
  }

  getSubnetById(subnetId: number, accountId: number): Observable<Subnet> {
    return this.http.get<Subnet>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/${subnetId}?accountId=${accountId}`
    );
  }

  getSubnetsByVpc(accountId: number, vpcId: string): Observable<SubnetsResponse> {
    return this.http.get<SubnetsResponse>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/list?accountId=${accountId}&vpcId=${encodeURIComponent(vpcId)}`
    );
  }

  updateSubnet(subnetId: number, accountId: number, request: UpdateSubnetRequest): Observable<Subnet> {
    return this.http.put<Subnet>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/update/${subnetId}?accountId=${accountId}`,
      request
    );
  }

  deleteSubnet(subnetId: number, accountId: number): Observable<DeleteSubnetResponse> {
    return this.http.delete<DeleteSubnetResponse>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/delete/${subnetId}?accountId=${accountId}`
    );
  }

  getSecurityGroups(accountId: number): Observable<SecurityGroupsResponse> {
    return this.http.get<SecurityGroupsResponse>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/list?accountId=${accountId}`
    );
  }

  syncSecurityGroups(accountId: number): Observable<SecurityGroupSyncResponse> {
    return this.http.post<SecurityGroupSyncResponse>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/sync?accountId=${accountId}`,
      null
    );
  }

  createSecurityGroup(accountId: number, request: CreateSecurityGroupRequest): Observable<SecurityGroup> {
    return this.http.post<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/create?accountId=${accountId}`,
      request
    );
  }

  getSecurityGroupById(id: number, accountId: number): Observable<SecurityGroup> {
    return this.http.get<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/${id}?accountId=${accountId}`
    );
  }

  updateSecurityGroup(id: number, accountId: number, request: UpdateSecurityGroupRequest): Observable<SecurityGroup> {
    return this.http.put<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/update/${id}?accountId=${accountId}`,
      request
    );
  }

  deleteSecurityGroup(id: number, accountId: number): Observable<DeleteSecurityGroupResponse> {
    return this.http.delete<DeleteSecurityGroupResponse>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/delete/${id}?accountId=${accountId}`
    );
  }

  addInboundRules(id: number, accountId: number, request: AddRulesRequest): Observable<SecurityGroup> {
    return this.http.post<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/${id}/inbound/add?accountId=${accountId}`,
      request
    );
  }

  removeInboundRules(id: number, accountId: number, request: RemoveRulesRequest): Observable<SecurityGroup> {
    return this.http.post<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/${id}/inbound/remove?accountId=${accountId}`,
      request
    );
  }

  addOutboundRules(id: number, accountId: number, request: AddRulesRequest): Observable<SecurityGroup> {
    return this.http.post<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/${id}/outbound/add?accountId=${accountId}`,
      request
    );
  }

  removeOutboundRules(id: number, accountId: number, request: RemoveRulesRequest): Observable<SecurityGroup> {
    return this.http.post<SecurityGroup>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/${id}/outbound/remove?accountId=${accountId}`,
      request
    );
  }

  getLogGroups(accountId: number): Observable<LogGroupsResponse> {
    return this.http.get<LogGroupsResponse>(
      `${this.apiUrl}/api/cloud/services/cloudwatch-logs/aws/log-groups/list?accountId=${accountId}`
    );
  }

  getEc2Instances(accountId: number): Observable<Ec2InstancesResponse> {
    return this.http.get<Ec2InstancesResponse>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/list?accountId=${accountId}`
    );
  }

  launchEc2Instance(accountId: number, request: LaunchEc2InstanceRequest): Observable<Ec2Instance[]> {
    return this.http.post<Ec2Instance[]>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/launch?accountId=${accountId}`,
      request
    );
  }

  syncEc2Instances(accountId: number): Observable<Ec2SyncResponse> {
    return this.http.post<Ec2SyncResponse>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/sync?accountId=${accountId}`,
      null
    );
  }

  getEc2InstanceById(instanceId: number, accountId: number): Observable<Ec2Instance> {
    return this.http.get<Ec2Instance>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/get/${instanceId}?accountId=${accountId}`
    );
  }

  updateEc2Instance(instanceId: number, accountId: number, request: UpdateEc2InstanceRequest): Observable<Ec2Instance> {
    return this.http.put<Ec2Instance>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/update/${instanceId}?accountId=${accountId}`,
      request
    );
  }

  startEc2Instance(instanceId: number, accountId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/start/${instanceId}?accountId=${accountId}`,
      null
    );
  }

  stopEc2Instance(instanceId: number, accountId: number, force = false): Observable<{ message: string }> {
    const url = force
      ? `${this.apiUrl}/api/cloud/services/ec2/aws/stop/${instanceId}?accountId=${accountId}&force=true`
      : `${this.apiUrl}/api/cloud/services/ec2/aws/stop/${instanceId}?accountId=${accountId}`;
    return this.http.post<{ message: string }>(url, null);
  }

  rebootEc2Instance(instanceId: number, accountId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/reboot/${instanceId}?accountId=${accountId}`,
      null
    );
  }

  terminateEc2Instance(instanceId: number, accountId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/api/cloud/services/ec2/aws/terminate/${instanceId}?accountId=${accountId}`
    );
  }

  getKeyPairs(accountId: number): Observable<KeyPairsResponse> {
    return this.http.get<KeyPairsResponse>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/list?accountId=${accountId}`
    );
  }

  getKeyPairById(keyPairId: number, accountId: number): Observable<KeyPair> {
    return this.http.get<KeyPair>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/get/${keyPairId}?accountId=${accountId}`
    );
  }

  createKeyPair(accountId: number, request: CreateKeyPairRequest): Observable<KeyPair> {
    return this.http.post<KeyPair>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/create?accountId=${accountId}`,
      request
    );
  }

  updateKeyPairTags(keyPairId: number, accountId: number, request: UpdateKeyPairTagsRequest): Observable<KeyPair> {
    return this.http.put<KeyPair>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/update/${keyPairId}?accountId=${accountId}`,
      request
    );
  }

  deleteKeyPair(keyPairId: number, accountId: number): Observable<KeyPairDeleteResponse> {
    return this.http.delete<KeyPairDeleteResponse>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/delete/${keyPairId}?accountId=${accountId}`
    );
  }

  downloadPrivateKey(keyPairId: number, accountId: number): Observable<DownloadPrivateKeyResponse> {
    return this.http.get<DownloadPrivateKeyResponse>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/download-private-key/${keyPairId}?accountId=${accountId}`
    );
  }

  syncKeyPairs(accountId: number): Observable<KeyPairSyncResponse> {
    return this.http.post<KeyPairSyncResponse>(
      `${this.apiUrl}/api/cloud/services/keypair/aws/sync?accountId=${accountId}`,
      null
    );
  }

  syncS3Buckets(accountId: number): Observable<FullSyncResult> {
    return this.http.post<FullSyncResult>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/sync?accountId=${accountId}`,
      null
    );
  }

  getS3Files(accountId: number): Observable<FileListResponse> {
    return this.http.get<FileListResponse>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files?accountId=${accountId}`
    );
  }

  getS3FilesByBucket(accountId: number, bucketId: number): Observable<FileListResponse> {
    return this.http.get<FileListResponse>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files?accountId=${accountId}&bucketId=${bucketId}`
    );
  }

  createS3Bucket(accountId: number, request: CreateBucketRequest): Observable<S3Bucket> {
    return this.http.post<S3Bucket>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/buckets?accountId=${accountId}`,
      request
    );
  }

  deleteS3Bucket(accountId: number, bucketId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/buckets/${bucketId}?accountId=${accountId}`
    );
  }

  syncBucketFiles(accountId: number, bucketId: number): Observable<BucketFileSyncResponse> {
    return this.http.post<BucketFileSyncResponse>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files/sync?accountId=${accountId}&bucketId=${bucketId}`,
      null
    );
  }

  uploadS3File(accountId: number, bucketId: number, file: File, folder: string): Observable<CloudFileResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);
    return this.http.post<CloudFileResponse>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files?accountId=${accountId}&bucketId=${bucketId}`,
      formData
    );
  }

  downloadS3File(fileId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files/${fileId}/download`,
      { responseType: 'blob' }
    );
  }

  updateS3File(fileId: number, file: File): Observable<CloudFileResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.put<CloudFileResponse>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files/${fileId}`,
      formData
    );
  }

  deleteS3File(fileId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/api/cloud/services/storage/aws/s3/files/${fileId}`
    );
  }

  // ── ECS Update / Delete / Sync / Run / Stop ──

  updateEcsService(serviceId: number, accountId: number, request: UpdateEcsServiceRequest): Observable<EcsService> {
    return this.http.put<EcsService>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/services/${serviceId}?accountId=${accountId}`,
      request
    );
  }

  deleteEcsService(serviceId: number, accountId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/services/${serviceId}?accountId=${accountId}`
    );
  }

  syncEcsServices(accountId: number, clusterName: string): Observable<SyncEcsServicesResult> {
    return this.http.post<SyncEcsServicesResult>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/services/sync?accountId=${accountId}&clusterName=${encodeURIComponent(clusterName)}`,
      null
    );
  }

  syncTasks(accountId: number, clusterName: string): Observable<SyncEcsTasksResult> {
    return this.http.post<SyncEcsTasksResult>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/tasks/sync?accountId=${accountId}&clusterName=${encodeURIComponent(clusterName)}`,
      null
    );
  }

  syncTaskDefinitions(accountId: number): Observable<SyncTaskDefinitionsResult> {
    return this.http.post<SyncTaskDefinitionsResult>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/task-definitions/sync?accountId=${accountId}`,
      null
    );
  }

  stopTask(taskId: number, accountId: number, request: StopTaskRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/tasks/${taskId}/stop?accountId=${accountId}`,
      request
    );
  }

  runTask(accountId: number, request: RunTaskRequest): Observable<EcsTask[]> {
    return this.http.post<EcsTask[]>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/tasks/run?accountId=${accountId}`,
      request
    );
  }

  deregisterTaskDefinition(tdId: number, accountId: number): Observable<EcsTaskDefinition> {
    return this.http.post<EcsTaskDefinition>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/task-definitions/${tdId}/deregister?accountId=${accountId}`,
      null
    );
  }

  deleteTaskDefinition(tdId: number, accountId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/api/cloud/services/ecs/aws/task-definitions/${tdId}?accountId=${accountId}`
    );
  }

  // ── Permissions ──

  getPermissionSummary(accountId: number): Observable<PermissionSummaryResponse> {
    return this.http.get<PermissionSummaryResponse>(
      `${this.apiUrl}/api/permissions/aws/summary?accountId=${accountId}`
    );
  }

  getPermissionPolicies(accountId: number): Observable<PoliciesResponse> {
    return this.http.get<PoliciesResponse>(
      `${this.apiUrl}/api/permissions/aws/policies?accountId=${accountId}`
    );
  }

  browseAvailablePolicies(accountId: number, scope: string, search: string): Observable<AvailablePoliciesResponse> {
    let url = `${this.apiUrl}/api/permissions/aws/policies/available?accountId=${accountId}`;
    if (scope) url += `&scope=${encodeURIComponent(scope)}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    return this.http.get<AvailablePoliciesResponse>(url);
  }

  attachPolicy(accountId: number, request: AttachPolicyRequest): Observable<PolicyActionResponse> {
    return this.http.post<PolicyActionResponse>(
      `${this.apiUrl}/api/permissions/aws/policies/attach?accountId=${accountId}`,
      request
    );
  }

  detachPolicy(accountId: number, policyArn: string): Observable<PolicyActionResponse> {
    return this.http.delete<PolicyActionResponse>(
      `${this.apiUrl}/api/permissions/aws/policies/detach?accountId=${accountId}&policyArn=${encodeURIComponent(policyArn)}`
    );
  }

  checkPermissions(accountId: number, request: CheckPermissionRequest): Observable<PermissionCheckResponse> {
    return this.http.post<PermissionCheckResponse>(
      `${this.apiUrl}/api/permissions/aws/check?accountId=${accountId}`,
      request
    );
  }

  syncPermissionPolicies(accountId: number): Observable<SyncPoliciesResponse> {
    return this.http.post<SyncPoliciesResponse>(
      `${this.apiUrl}/api/permissions/aws/sync?accountId=${accountId}`,
      null
    );
  }

  listPermissionPolicies(accountId: number): Observable<ListPoliciesResponse> {
    return this.http.get<ListPoliciesResponse>(
      `${this.apiUrl}/api/permissions/aws/list?accountId=${accountId}`
    );
  }

  // ── Internet Gateway ──

  getInternetGateways(accountId: number): Observable<InternetGatewaysResponse> {
    return this.http.get<InternetGatewaysResponse>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/list?accountId=${accountId}`
    );
  }

  getInternetGatewayById(id: number, accountId: number): Observable<InternetGateway> {
    return this.http.get<InternetGateway>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/${id}?accountId=${accountId}`
    );
  }

  getInternetGatewayForVpc(vpcId: string, accountId: number): Observable<InternetGatewayVpcResponse> {
    return this.http.get<InternetGatewayVpcResponse>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/vpc/${encodeURIComponent(vpcId)}?accountId=${accountId}`
    );
  }

  createInternetGateway(accountId: number, request: CreateInternetGatewayRequest): Observable<InternetGateway> {
    return this.http.post<InternetGateway>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/create?accountId=${accountId}`,
      request
    );
  }

  updateInternetGateway(id: number, accountId: number, request: UpdateInternetGatewayRequest): Observable<InternetGateway> {
    return this.http.put<InternetGateway>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/update/${id}?accountId=${accountId}`,
      request
    );
  }

  deleteInternetGateway(id: number, accountId: number): Observable<InternetGatewayDeleteResponse> {
    return this.http.delete<InternetGatewayDeleteResponse>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/delete/${id}?accountId=${accountId}`
    );
  }

  attachInternetGateway(id: number, accountId: number, request: AttachInternetGatewayRequest): Observable<InternetGateway> {
    return this.http.post<InternetGateway>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/attach/${id}?accountId=${accountId}`,
      request
    );
  }

  detachInternetGateway(id: number, accountId: number, request: DetachInternetGatewayRequest): Observable<InternetGateway> {
    return this.http.post<InternetGateway>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/detach/${id}?accountId=${accountId}`,
      request
    );
  }

  syncInternetGateways(accountId: number): Observable<InternetGatewaySyncResponse> {
    return this.http.post<InternetGatewaySyncResponse>(
      `${this.apiUrl}/api/cloud/services/internet-gateway/aws/sync?accountId=${accountId}`,
      null
    );
  }
}
