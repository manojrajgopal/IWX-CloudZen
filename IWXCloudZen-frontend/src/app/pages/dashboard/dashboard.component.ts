import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { CloudAccountService } from '../../services/cloud-account.service';
import { CloudServicesService } from '../../services/cloud-services.service';
import { CloudAccount } from '../../models/cloud-account.model';
import {
  Cluster, S3Bucket, Vpc, EcrRepository, EcsService,
  Subnet, SecurityGroup, LogGroup, Ec2Instance
} from '../../models/cloud-services.model';

interface Metric {
  label: string;
  value: string;
}

export interface ServiceCategory {
  key: string;
  title: string;
  icon: string;
  count: number;
  items: any[];
  loading: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  currentUser: any = null;
  metrics: Metric[] = [
    { label: 'Active Services', value: '—' },
    { label: 'Connected Clouds', value: '—' },
    { label: 'Total Resources', value: '—' },
    { label: 'Health Score', value: '98%' }
  ];

  serviceCategories: ServiceCategory[] = [
    { key: 'clusters', title: 'Clusters', icon: 'M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z', count: 0, items: [], loading: true },
    { key: 'storage', title: 'Cloud Storage', icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4', count: 0, items: [], loading: true },
    { key: 'vpcs', title: 'VPCs', icon: 'M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z', count: 0, items: [], loading: true },
    { key: 'ecr', title: 'ECR Repositories', icon: 'M21 16.5V7.5a2.25 2.25 0 00-1.133-1.957l-6.75-3.857a2.25 2.25 0 00-2.234 0l-6.75 3.857A2.25 2.25 0 003 7.5v9a2.25 2.25 0 001.133 1.957l6.75 3.857a2.25 2.25 0 002.234 0l6.75-3.857A2.25 2.25 0 0021 16.5zM12 8.25l-6.75-3.857M12 8.25v13.5m0-13.5L18.75 4.393', count: 0, items: [], loading: true },
    { key: 'ecs', title: 'ECS Services', icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10', count: 0, items: [], loading: true },
    { key: 'subnets', title: 'Subnets', icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zm10 0a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zm10 0a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z', count: 0, items: [], loading: true },
    { key: 'securityGroups', title: 'Security Groups', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', count: 0, items: [], loading: true },
    { key: 'logGroups', title: 'CloudWatch Logs', icon: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z', count: 0, items: [], loading: true },
    { key: 'ec2', title: 'EC2 Instances', icon: 'M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2m-2-4h.01M17 16h.01', count: 0, items: [], loading: true }
  ];

  accounts: CloudAccount[] = [];
  loadingServices = true;
  Math = Math;

  // Modal state
  selectedService: ServiceCategory | null = null;
  showDetailModal = false;

  constructor(
    private authService: AuthService,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getUser();
    this.loadAccounts();
  }

  private loadAccounts(): void {
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.metrics[1].value = accounts.length.toString();
        this.loadAllServices(accounts);
      },
      error: () => {
        this.metrics[1].value = '0';
        this.loadingServices = false;
        this.serviceCategories.forEach(s => s.loading = false);
      }
    });
  }

  private loadAllServices(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loadingServices = false;
      this.serviceCategories.forEach(s => s.loading = false);
      this.updateMetrics();
      return;
    }

    const accountIds = accounts.map(a => a.id);

    // For each service category, call APIs for ALL connected accounts and merge results
    const allRequests: { [key: string]: Observable<any>[] } = {};
    this.serviceCategories.forEach(cat => allRequests[cat.key] = []);

    for (const accountId of accountIds) {
      allRequests['clusters'].push(
        this.cloudServicesService.getClusters(accountId).pipe(catchError(() => of({ clusters: [] })))
      );
      allRequests['storage'].push(
        this.cloudServicesService.getS3Buckets(accountId).pipe(catchError(() => of({ buckets: [] })))
      );
      allRequests['vpcs'].push(
        this.cloudServicesService.getVpcs(accountId).pipe(catchError(() => of({ vpcs: [] })))
      );
      allRequests['ecr'].push(
        this.cloudServicesService.getEcrRepositories(accountId).pipe(catchError(() => of({ repositories: [] })))
      );
      allRequests['ecs'].push(
        this.cloudServicesService.getEcsServices(accountId).pipe(catchError(() => of({ services: [] })))
      );
      allRequests['subnets'].push(
        this.cloudServicesService.getSubnets(accountId).pipe(catchError(() => of({ subnets: [] })))
      );
      allRequests['securityGroups'].push(
        this.cloudServicesService.getSecurityGroups(accountId).pipe(catchError(() => of({ securityGroups: [] })))
      );
      allRequests['logGroups'].push(
        this.cloudServicesService.getLogGroups(accountId).pipe(catchError(() => of({ logGroups: [] })))
      );
      allRequests['ec2'].push(
        this.cloudServicesService.getEc2Instances(accountId).pipe(catchError(() => of({ instances: [] })))
      );
    }

    // Clusters
    forkJoin(allRequests['clusters']).subscribe(results => {
      const cat = this.getCategory('clusters');
      cat.items = results.flatMap((r: any) => r.clusters || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // S3 Buckets
    forkJoin(allRequests['storage']).subscribe(results => {
      const cat = this.getCategory('storage');
      cat.items = results.flatMap((r: any) => r.buckets || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // VPCs
    forkJoin(allRequests['vpcs']).subscribe(results => {
      const cat = this.getCategory('vpcs');
      cat.items = results.flatMap((r: any) => r.vpcs || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // ECR
    forkJoin(allRequests['ecr']).subscribe(results => {
      const cat = this.getCategory('ecr');
      cat.items = results.flatMap((r: any) => r.repositories || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // ECS Services
    forkJoin(allRequests['ecs']).subscribe(results => {
      const cat = this.getCategory('ecs');
      cat.items = results.flatMap((r: any) => r.services || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // Subnets
    forkJoin(allRequests['subnets']).subscribe(results => {
      const cat = this.getCategory('subnets');
      cat.items = results.flatMap((r: any) => r.subnets || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // Security Groups
    forkJoin(allRequests['securityGroups']).subscribe(results => {
      const cat = this.getCategory('securityGroups');
      cat.items = results.flatMap((r: any) => r.securityGroups || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // CloudWatch Log Groups
    forkJoin(allRequests['logGroups']).subscribe(results => {
      const cat = this.getCategory('logGroups');
      cat.items = results.flatMap((r: any) => r.logGroups || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });

    // EC2 Instances
    forkJoin(allRequests['ec2']).subscribe(results => {
      const cat = this.getCategory('ec2');
      cat.items = results.flatMap((r: any) => r.instances || []);
      cat.count = cat.items.length;
      cat.loading = false;
      this.updateMetrics();
    });
  }

  private getCategory(key: string): ServiceCategory {
    return this.serviceCategories.find(c => c.key === key)!;
  }

  private updateMetrics(): void {
    const totalResources = this.serviceCategories.reduce((sum, c) => sum + c.count, 0);
    this.metrics[2].value = totalResources.toString();

    const activeServiceCount = this.serviceCategories.filter(c => c.count > 0).length;
    this.metrics[0].value = activeServiceCount.toString();

    this.loadingServices = this.serviceCategories.some(c => c.loading);
  }

  openServiceDetail(category: ServiceCategory): void {
    // Navigate to dedicated page for services that have one
    if (category.key === 'storage') {
      this.router.navigate(['/dashboard/cloud-storage']);
      return;
    }
    if (category.key === 'clusters') {
      this.router.navigate(['/dashboard/clusters']);
      return;
    }
    if (category.key === 'vpcs') {
      this.router.navigate(['/dashboard/vpcs']);
      return;
    }
    if (category.key === 'ecr') {
      this.router.navigate(['/dashboard/ecr']);
      return;
    }
    if (category.key === 'ecs') {
      this.router.navigate(['/dashboard/ecs']);
      return;
    }
    if (category.key === 'subnets') {
      this.router.navigate(['/dashboard/subnets']);
      return;
    }
    if (category.key === 'securityGroups') {
      this.router.navigate(['/dashboard/security-groups']);
      return;
    }
    if (category.key === 'logGroups') {
      this.router.navigate(['/dashboard/cloudwatch-logs']);
      return;
    }
    if (category.key === 'ec2') {
      this.router.navigate(['/dashboard/ec2-instances']);
      return;
    }
    this.selectedService = category;
    this.showDetailModal = true;
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    setTimeout(() => this.selectedService = null, 300);
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getItemName(item: any, key: string): string {
    switch (key) {
      case 'clusters': return item.name;
      case 'storage': return item.name;
      case 'vpcs': return item.name || item.vpcId;
      case 'ecr': return item.repositoryName;
      case 'ecs': return item.serviceName;
      case 'subnets': return item.name || item.subnetId;
      case 'securityGroups': return item.groupName;
      case 'logGroups': return item.logGroupName;
      case 'ec2': return item.name || item.instanceId;
      default: return 'Unknown';
    }
  }

  getItemStatus(item: any, key: string): string {
    switch (key) {
      case 'clusters': return item.status;
      case 'storage': return item.status;
      case 'vpcs': return item.state;
      case 'ecr': return 'Active';
      case 'ecs': return item.status;
      case 'subnets': return item.state;
      case 'securityGroups': return 'Active';
      case 'logGroups': return item.logGroupClass;
      case 'ec2': return item.state;
      default: return '—';
    }
  }

  getItemDetail(item: any, key: string): string {
    switch (key) {
      case 'clusters': return `Provider: ${item.provider}`;
      case 'storage': return `Region: ${item.region}`;
      case 'vpcs': return `CIDR: ${item.cidrBlock}`;
      case 'ecr': return `URI: ${item.repositoryUri || '—'}`;
      case 'ecs': return `Cluster: ${item.clusterName} · Launch: ${item.launchType}`;
      case 'subnets': return `AZ: ${item.availabilityZone} · CIDR: ${item.cidrBlock}`;
      case 'securityGroups': return `VPC: ${item.vpcId} · Rules: ${item.inboundRules?.length || 0} in / ${item.outboundRules?.length || 0} out`;
      case 'logGroups': return `Retention: ${item.retentionInDays ? item.retentionInDays + ' days' : 'Never'} · Size: ${this.formatBytes(item.storedBytes)}`;
      case 'ec2': return `Type: ${item.instanceType} · IP: ${item.publicIpAddress || item.privateIpAddress}`;
      default: return '';
    }
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'available', 'created', 'standard'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-500';
    if (['stopped', 'terminated', 'deleted', 'inactive'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  private formatBytes(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}