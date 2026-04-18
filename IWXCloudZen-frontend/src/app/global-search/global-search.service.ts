import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, forkJoin, of } from 'rxjs';
import { map, catchError, switchMap, take } from 'rxjs/operators';
import { CloudServicesService } from '../services/cloud-services.service';
import { CloudAccountService } from '../services/cloud-account.service';
import { SearchResult, SearchResultCategory, SearchFilter } from './search-result.model';
import { Router } from '@angular/router';

// Static navigation pages for search
const STATIC_PAGES: SearchResult[] = [
  { category: 'page', title: 'Dashboard', subtitle: 'Main dashboard overview', icon: '📊', route: '/dashboard' },
  { category: 'page', title: 'Cloud Storage', subtitle: 'Manage S3 buckets and files', icon: '🗄️', route: '/dashboard/cloud-storage' },
  { category: 'page', title: 'Create Bucket', subtitle: 'Create a new S3 bucket', icon: '➕', route: '/dashboard/cloud-storage/create' },
  { category: 'page', title: 'Clusters', subtitle: 'View and manage clusters', icon: '🔗', route: '/dashboard/clusters' },
  { category: 'page', title: 'VPCs', subtitle: 'Virtual Private Clouds', icon: '🌐', route: '/dashboard/vpcs' },
  { category: 'page', title: 'ECR', subtitle: 'Elastic Container Registry', icon: '📦', route: '/dashboard/ecr' },
  { category: 'page', title: 'ECS', subtitle: 'Elastic Container Service', icon: '🚀', route: '/dashboard/ecs' },
  { category: 'page', title: 'Subnets', subtitle: 'Network subnets', icon: '🔌', route: '/dashboard/subnets' },
  { category: 'page', title: 'Security Groups', subtitle: 'Firewall rules and security groups', icon: '🛡️', route: '/dashboard/security-groups' },
  { category: 'page', title: 'CloudWatch Logs', subtitle: 'Monitor and view log groups', icon: '📋', route: '/dashboard/cloudwatch-logs' },
  { category: 'page', title: 'EC2 Instances', subtitle: 'Virtual machines', icon: '🖥️', route: '/dashboard/ec2-instances' },
  { category: 'page', title: 'Profile', subtitle: 'User profile settings', icon: '👤', route: '/profile' },
];

// Static cloud providers
const STATIC_CLOUDS: SearchResult[] = [
  { category: 'cloud', title: 'AWS', subtitle: 'Amazon Web Services', icon: '☁️', route: '/dashboard' },
  { category: 'cloud', title: 'Azure', subtitle: 'Microsoft Azure', icon: '☁️', route: '/dashboard' },
  { category: 'cloud', title: 'Google Cloud', subtitle: 'Google Cloud Platform', icon: '☁️', route: '/dashboard' },
  { category: 'cloud', title: 'DigitalOcean', subtitle: 'DigitalOcean Cloud', icon: '☁️', route: '/dashboard' },
  { category: 'cloud', title: 'IBM Cloud', subtitle: 'IBM Cloud Platform', icon: '☁️', route: '/dashboard' },
];

// Static services
const STATIC_SERVICES: SearchResult[] = [
  { category: 'service', title: 'Kubernetes Manager', subtitle: 'Deploy and scale containers across clouds', icon: '⚙️', route: '/dashboard/clusters', meta: { clouds: 'AWS,Azure,GCP' } },
  { category: 'service', title: 'Serverless Framework', subtitle: 'Build and deploy serverless apps', icon: '⚡', route: '/dashboard' },
  { category: 'service', title: 'Multi‑Cloud Storage', subtitle: 'Unified storage with tiering and backup', icon: '💾', route: '/dashboard/cloud-storage' },
  { category: 'service', title: 'Cloud Cost Optimizer', subtitle: 'Reduce cloud spend with AI', icon: '💰', route: '/dashboard' },
  { category: 'service', title: 'AI/ML Platform', subtitle: 'Train and deploy models across clouds', icon: '🤖', route: '/dashboard' },
  { category: 'service', title: 'Security Posture Manager', subtitle: 'Compliance and threat detection', icon: '🔒', route: '/dashboard/security-groups' },
];

const CATEGORY_LABELS: Record<SearchResultCategory, string> = {
  'cluster': 'Clusters',
  'bucket': 'Buckets',
  'vpc': 'VPCs',
  'ecr': 'ECR Repositories',
  'ecs': 'ECS Services',
  'subnet': 'Subnets',
  'security-group': 'Security Groups',
  'log-group': 'Log Groups',
  'ec2': 'EC2 Instances',
  'file': 'Files',
  'service': 'Services',
  'cloud': 'Clouds',
  'page': 'Pages',
};

@Injectable({ providedIn: 'root' })
export class GlobalSearchService {
  private resultsSubject = new BehaviorSubject<SearchResult[]>([]);
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private lastFilterSubject = new BehaviorSubject<SearchFilter>({ query: '', cloud: 'all', category: 'all' });

  results$ = this.resultsSubject.asObservable();
  loading$ = this.loadingSubject.asObservable();
  lastFilter$ = this.lastFilterSubject.asObservable();

  constructor(
    private cloudServices: CloudServicesService,
    private cloudAccounts: CloudAccountService,
    private router: Router
  ) {}

  getCategoryLabel(cat: SearchResultCategory): string {
    return CATEGORY_LABELS[cat] || cat;
  }

  clearResults(): void {
    this.resultsSubject.next([]);
    this.loadingSubject.next(false);
  }

  getAllCategories(): { value: SearchResultCategory | 'all'; label: string }[] {
    return [
      { value: 'all', label: 'All' },
      ...Object.entries(CATEGORY_LABELS).map(([value, label]) => ({
        value: value as SearchResultCategory,
        label,
      })),
    ];
  }

  search(filter: SearchFilter): void {
    this.lastFilterSubject.next(filter);
    this.loadingSubject.next(true);

    const q = filter.query.toLowerCase().trim();

    if (!q) {
      this.resultsSubject.next([]);
      this.loadingSubject.next(false);
      return;
    }

    // Get static results first
    const staticResults = this.searchStatic(q, filter);

    // Try to fetch live resource data from the API
    this.cloudAccounts.getDefaultAccount().pipe(
      take(1),
      switchMap(account => this.fetchLiveResults(account.id, q, filter)),
      catchError(() => of([] as SearchResult[])),
    ).subscribe(liveResults => {
      const combined = [...staticResults, ...liveResults];
      const filtered = filter.category === 'all'
        ? combined
        : combined.filter(r => r.category === filter.category);
      this.resultsSubject.next(filtered);
      this.loadingSubject.next(false);
    });
  }

  navigateTo(route: string): void {
    const segments = route.split('/').filter(s => s.length > 0);
    this.router.navigate(['/', ...segments]);
  }

  private searchStatic(q: string, filter: SearchFilter): SearchResult[] {
    const all = [...STATIC_PAGES, ...STATIC_SERVICES, ...STATIC_CLOUDS];
    return all.filter(item => {
      const matchesQuery =
        item.title.toLowerCase().includes(q) ||
        item.subtitle.toLowerCase().includes(q) ||
        item.category.toLowerCase().includes(q) ||
        (item.meta?.['clouds']?.toLowerCase().includes(q) ?? false);

      const matchesCloud =
        filter.cloud === 'all' ||
        item.meta?.['clouds']?.toLowerCase().includes(filter.cloud.toLowerCase()) ||
        item.title.toLowerCase().includes(filter.cloud.toLowerCase()) ||
        item.category === 'page'; // Pages always match cloud filter

      return matchesQuery && matchesCloud;
    });
  }

  private fetchLiveResults(accountId: number, q: string, filter: SearchFilter): Observable<SearchResult[]> {
    return forkJoin({
      clusters: this.cloudServices.getClusters(accountId).pipe(catchError(() => of({ clusters: [] }))),
      buckets: this.cloudServices.getS3Buckets(accountId).pipe(catchError(() => of({ buckets: [] }))),
      vpcs: this.cloudServices.getVpcs(accountId).pipe(catchError(() => of({ vpcs: [] }))),
      ecr: this.cloudServices.getEcrRepositories(accountId).pipe(catchError(() => of({ totalRepositories: 0, repositories: [] }))),
      ecs: this.cloudServices.getEcsServices(accountId).pipe(catchError(() => of({ clusterName: '', totalCount: 0, services: [] }))),
      subnets: this.cloudServices.getSubnets(accountId).pipe(catchError(() => of({ totalCount: 0, vpcIdFilter: null, subnets: [] }))),
      securityGroups: this.cloudServices.getSecurityGroups(accountId).pipe(catchError(() => of({ totalCount: 0, vpcIdFilter: null, securityGroups: [] }))),
      logGroups: this.cloudServices.getLogGroups(accountId).pipe(catchError(() => of({ logGroups: [] }))),
      ec2: this.cloudServices.getEc2Instances(accountId).pipe(catchError(() => of({ instances: [] }))),
      files: this.cloudServices.getS3Files(accountId).pipe(catchError(() => of({ files: [] }))),
    }).pipe(
      map(data => {
        const results: SearchResult[] = [];

        // Clusters
        data.clusters.clusters
          .filter(c => this.matchesText(q, c.name, c.clusterArn ?? '', c.status, c.provider))
          .forEach(c => results.push({
            category: 'cluster', title: c.name,
            subtitle: `${c.provider} · ${c.status}`,
            icon: '🔗', route: '/dashboard/clusters',
            meta: { provider: c.provider },
          }));

        // S3 Buckets
        data.buckets.buckets
          .filter(b => this.matchesText(q, b.name, b.region, b.status, b.provider))
          .forEach(b => results.push({
            category: 'bucket', title: b.name,
            subtitle: `${b.provider} · ${b.region} · ${b.status}`,
            icon: '🪣', route: `/dashboard/cloud-storage/${b.id}`,
            meta: { provider: b.provider },
          }));

        // VPCs
        data.vpcs.vpcs
          .filter(v => this.matchesText(q, v.name, v.vpcId, v.cidrBlock, v.state, v.provider))
          .forEach(v => results.push({
            category: 'vpc', title: v.name || v.vpcId,
            subtitle: `${v.provider} · ${v.cidrBlock} · ${v.state}`,
            icon: '🌐', route: '/dashboard/vpcs',
            meta: { provider: v.provider },
          }));

        // ECR Repos
        data.ecr.repositories
          .filter(r => this.matchesText(q, r.repositoryName, r.repositoryUri, r.provider))
          .forEach(r => results.push({
            category: 'ecr', title: r.repositoryName,
            subtitle: `${r.provider} · ${r.repositoryUri}`,
            icon: '📦', route: '/dashboard/ecr',
            meta: { provider: r.provider },
          }));

        // ECS Services
        data.ecs.services
          .filter(s => this.matchesText(q, s.serviceName, s.clusterName, s.status, s.launchType, s.provider))
          .forEach(s => results.push({
            category: 'ecs', title: s.serviceName,
            subtitle: `${s.provider} · ${s.clusterName} · ${s.status}`,
            icon: '🚀', route: '/dashboard/ecs',
            meta: { provider: s.provider },
          }));

        // Subnets
        data.subnets.subnets
          .filter(s => this.matchesText(q, s.name, s.subnetId, s.vpcId, s.cidrBlock, s.availabilityZone, s.provider))
          .forEach(s => results.push({
            category: 'subnet', title: s.name || s.subnetId,
            subtitle: `${s.provider} · ${s.cidrBlock} · ${s.availabilityZone}`,
            icon: '🔌', route: '/dashboard/subnets',
            meta: { provider: s.provider },
          }));

        // Security Groups
        data.securityGroups.securityGroups
          .filter(sg => this.matchesText(q, sg.groupName, sg.securityGroupId, sg.description, sg.vpcId, sg.provider))
          .forEach(sg => results.push({
            category: 'security-group', title: sg.groupName,
            subtitle: `${sg.provider} · ${sg.securityGroupId} · ${sg.vpcId}`,
            icon: '🛡️', route: '/dashboard/security-groups',
            meta: { provider: sg.provider },
          }));

        // Log Groups
        data.logGroups.logGroups
          .filter(lg => this.matchesText(q, lg.logGroupName, lg.arn, lg.provider))
          .forEach(lg => results.push({
            category: 'log-group', title: lg.logGroupName,
            subtitle: `${lg.provider} · ${lg.logGroupClass}`,
            icon: '📋', route: '/dashboard/cloudwatch-logs',
            meta: { provider: lg.provider },
          }));

        // EC2 Instances
        data.ec2.instances
          .filter(i => this.matchesText(q, i.name, i.instanceId, i.instanceType, i.state, i.publicIpAddress ?? '', i.privateIpAddress, i.provider))
          .forEach(i => results.push({
            category: 'ec2', title: i.name || i.instanceId,
            subtitle: `${i.provider} · ${i.instanceType} · ${i.state}`,
            icon: '🖥️', route: '/dashboard/ec2-instances',
            meta: { provider: i.provider },
          }));

        // S3 Files
        data.files.files
          .filter(f => this.matchesText(q, f.fileName, f.bucketName, f.contentType, f.folder, f.provider))
          .forEach(f => results.push({
            category: 'file', title: f.fileName,
            subtitle: `${f.bucketName} · ${f.folder || '/'} · ${this.formatBytes(f.size)}`,
            icon: '📄', route: '/dashboard/cloud-storage',
            meta: { provider: f.provider },
          }));

        // Filter by cloud if needed
        if (filter.cloud !== 'all') {
          return results.filter(r =>
            r.meta?.['provider']?.toLowerCase().includes(filter.cloud.toLowerCase())
          );
        }

        return results;
      })
    );
  }

  private matchesText(query: string, ...fields: string[]): boolean {
    const q = query.toLowerCase();
    return fields.some(f => f?.toLowerCase().includes(q));
  }

  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
