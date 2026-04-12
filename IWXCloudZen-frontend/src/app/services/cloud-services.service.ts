import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClustersResponse,
  BucketsResponse,
  VpcsResponse,
  EcrRepositoriesResponse,
  EcsServicesResponse,
  SubnetsResponse,
  SecurityGroupsResponse,
  LogGroupsResponse,
  Ec2InstancesResponse,
  FullSyncResult,
  FileListResponse,
  BucketFileSyncResponse,
  S3Bucket,
  CloudFileResponse,
  CreateBucketRequest
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

  getSubnets(accountId: number): Observable<SubnetsResponse> {
    return this.http.get<SubnetsResponse>(
      `${this.apiUrl}/api/cloud/services/subnet/aws/list?accountId=${accountId}`
    );
  }

  getSecurityGroups(accountId: number): Observable<SecurityGroupsResponse> {
    return this.http.get<SecurityGroupsResponse>(
      `${this.apiUrl}/api/cloud/services/security-groups/aws/list?accountId=${accountId}`
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
}
