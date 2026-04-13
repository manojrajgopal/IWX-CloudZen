import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { EcsService, EcsTaskDefinition, EcsTask } from '../../../../models/cloud-services.model';

interface ContainerDefinition {
  Name: string;
  Image: string;
  Cpu: number;
  Memory: number;
  Essential: boolean;
  PortMappings: { ContainerPort: number; HostPort: number; Protocol: string }[];
  Environment: { Name: string; Value: string }[];
}

interface NetworkConfig {
  Subnets: string[];
  SecurityGroups: string[];
  AssignPublicIp: string;
}

@Component({
  selector: 'app-ecs-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ecs-overview.component.html',
  styleUrls: ['./ecs-overview.component.css']
})
export class EcsOverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  service: EcsService | null = null;
  taskDefinition: EcsTaskDefinition | null = null;
  tasks: EcsTask[] = [];
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];

  // Parsed data
  containerDefinitions: ContainerDefinition[] = [];
  networkConfig: NetworkConfig | null = null;

  // Section collapse states
  collapsedSections: Record<string, boolean> = {};

  // Copy tooltip
  copiedField: string | null = null;
  private copiedTimeout: any;

  // Tasks view
  showAllTasks = false;
  selectedTask: EcsTask | null = null;

  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.error = 'Invalid ECS Service ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(serviceId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findService(accounts, serviceId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findService(accounts: CloudAccount[], serviceId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getEcsServices(a.id).pipe(catchError(() => of({ services: [] })))
    );

    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const allServices: EcsService[] = results.flatMap((r: any) => r.services || []);
        this.service = allServices.find(s => s.id === serviceId) || null;

        if (this.service) {
          this.account = accounts.find(a => a.id === this.service!.cloudAccountId) || null;
          this.parseNetworkConfig();
          this.loadRelatedData();
        } else {
          this.error = 'ECS Service not found';
          this.loading = false;
        }
      },
      error: () => {
        this.error = 'Failed to load ECS data';
        this.loading = false;
      }
    });
  }

  private loadRelatedData(): void {
    if (!this.service) {
      this.loading = false;
      return;
    }

    const accountId = this.service.cloudAccountId;

    forkJoin({
      taskDefs: this.cloudServicesService.getEcsTaskDefinitions(accountId).pipe(catchError(() => of({ taskDefinitions: [] }))),
      tasks: this.cloudServicesService.getEcsTasks(accountId).pipe(catchError(() => of({ tasks: [] })))
    }).subscribe({
      next: ({ taskDefs, tasks }: any) => {
        // Find matching task definition by ARN
        const allTaskDefs: EcsTaskDefinition[] = taskDefs.taskDefinitions || [];
        this.taskDefinition = allTaskDefs.find(td =>
          td.taskDefinitionArn === this.service!.taskDefinition
        ) || null;

        if (this.taskDefinition) {
          this.parseContainerDefinitions();
        }

        // Filter tasks belonging to this service
        const allTasks: EcsTask[] = tasks.tasks || [];
        this.tasks = allTasks.filter(t =>
          t.group === `service:${this.service!.serviceName}` &&
          t.clusterName === this.service!.clusterName
        );

        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  private parseNetworkConfig(): void {
    if (!this.service?.networkConfigurationJson) return;
    try {
      this.networkConfig = JSON.parse(this.service.networkConfigurationJson);
    } catch {
      this.networkConfig = null;
    }
  }

  private parseContainerDefinitions(): void {
    if (!this.taskDefinition?.containerDefinitionsJson) return;
    try {
      this.containerDefinitions = JSON.parse(this.taskDefinition.containerDefinitionsJson);
    } catch {
      this.containerDefinitions = [];
    }
  }

  refreshData(): void {
    if (this.service) {
      this.loadData(this.service.id);
    }
  }

  // ── Section Toggle ──

  toggleSection(section: string): void {
    this.collapsedSections[section] = !this.collapsedSections[section];
  }

  isSectionCollapsed(section: string): boolean {
    return !!this.collapsedSections[section];
  }

  // ── Copy ──

  copyToClipboard(text: string, field: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedField = field;
      if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
      this.copiedTimeout = setTimeout(() => this.copiedField = null, 2000);
    });
  }

  // ── Display Helpers ──

  getTaskHealth(): number {
    if (!this.service || !this.service.desiredCount || this.service.desiredCount === 0) return 100;
    return Math.round((this.service.runningCount / this.service.desiredCount) * 100);
  }

  getStatusBgClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'connected'].includes(s)) return 'bg-green-50 border-green-200';
    if (['pending', 'draining', 'provisioning'].includes(s)) return 'bg-yellow-50 border-yellow-200';
    if (['inactive', 'stopped', 'failed', 'deprovisioning'].includes(s)) return 'bg-red-50 border-red-200';
    return 'bg-gray-50 border-gray-200';
  }

  getStatusTextClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'connected'].includes(s)) return 'text-green-700';
    if (['pending', 'draining', 'provisioning'].includes(s)) return 'text-yellow-700';
    if (['inactive', 'stopped', 'failed', 'deprovisioning'].includes(s)) return 'text-red-700';
    return 'text-gray-600';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'connected'].includes(s)) return 'bg-green-500';
    if (['pending', 'draining', 'provisioning'].includes(s)) return 'bg-yellow-500';
    if (['inactive', 'stopped', 'failed', 'deprovisioning'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'Amazon Web Services';
      case 'AZURE': return 'Microsoft Azure';
      case 'GCP': return 'Google Cloud Platform';
      default: return provider || 'Unknown';
    }
  }

  getProviderShort(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS';
      case 'AZURE': return 'Azure';
      case 'GCP': return 'GCP';
      default: return provider || '—';
    }
  }

  getProviderBgClass(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'bg-orange-50 border-orange-200 text-orange-700';
      case 'AZURE': return 'bg-blue-50 border-blue-200 text-blue-700';
      case 'GCP': return 'bg-red-50 border-red-200 text-red-700';
      default: return 'bg-gray-50 border-gray-200 text-gray-600';
    }
  }

  getLaunchTypeClass(type: string): string {
    switch (type?.toUpperCase()) {
      case 'FARGATE': return 'bg-blue-50 border-blue-200 text-blue-700';
      case 'EC2': return 'bg-orange-50 border-orange-200 text-orange-700';
      case 'EXTERNAL': return 'bg-purple-50 border-purple-200 text-purple-700';
      default: return 'bg-gray-50 border-gray-200 text-gray-600';
    }
  }

  getTaskStatusBgClass(status: string): string {
    const s = status?.toLowerCase();
    if (['running'].includes(s)) return 'bg-green-50 text-green-700';
    if (['pending', 'provisioning'].includes(s)) return 'bg-yellow-50 text-yellow-700';
    if (['stopped', 'deprovisioning'].includes(s)) return 'bg-red-50 text-red-700';
    return 'bg-gray-50 text-gray-500';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
  }

  formatRelativeTime(dateStr: string): string {
    if (!dateStr) return '—';
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins !== 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`;
    if (diffDays < 30) return `${diffDays} day${diffDays !== 1 ? 's' : ''} ago`;
    return this.formatDate(dateStr);
  }

  getUptime(dateStr: string): string {
    if (!dateStr) return '—';
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const days = Math.floor(diffMs / 86400000);
    const hours = Math.floor((diffMs % 86400000) / 3600000);
    const minutes = Math.floor((diffMs % 3600000) / 60000);

    if (days > 0) return `${days}d ${hours}h ${minutes}m`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
  }

  getShortTaskId(taskArn: string): string {
    if (!taskArn) return '—';
    const parts = taskArn.split('/');
    return parts[parts.length - 1]?.substring(0, 12) || '—';
  }

  get runningTasks(): EcsTask[] {
    return this.tasks.filter(t => t.lastStatus?.toLowerCase() === 'running' || t.desiredStatus?.toLowerCase() === 'running');
  }

  get stoppedTasks(): EcsTask[] {
    return this.tasks.filter(t => t.lastStatus?.toLowerCase() === 'stopped');
  }

  get pendingTasks(): EcsTask[] {
    return this.tasks.filter(t => t.lastStatus?.toLowerCase() === 'pending');
  }

  get displayedTasks(): EcsTask[] {
    return this.showAllTasks ? this.tasks : this.tasks.slice(0, 5);
  }

  selectTask(task: EcsTask): void {
    this.selectedTask = this.selectedTask?.id === task.id ? null : task;
  }

  trackByTaskId(index: number, task: EcsTask): number {
    return task.id;
  }
}
