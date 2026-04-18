import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { EcsTask, Cluster } from '../../../../models/cloud-services.model';
import { GenericFilterPipe } from './ecs-tasks.pipes';

type ViewMode = 'list' | 'grid';
type SortField = 'clusterName' | 'lastStatus' | 'launchType' | 'createdAt' | 'stoppedAt';
type SortDir = 'asc' | 'desc';

interface TaskMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-ecs-tasks',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, GenericFilterPipe],
  templateUrl: './ecs-tasks.component.html',
  styleUrls: ['./ecs-tasks.component.css']
})
export class EcsTasksComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  tasks: EcsTask[] = [];
  clusters: Cluster[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedStatus = 'all';
  selectedCluster = 'all';
  selectedAccount = 'all';
  selectedLaunchType = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'createdAt';
  sortDir: SortDir = 'desc';

  // Detail panel
  selectedTask: EcsTask | null = null;
  showDetailPanel = false;

  // Sync
  syncing = false;
  syncClusterName = '';

  // Stop task
  showStopDialog = false;
  stopTarget: EcsTask | null = null;
  stopReason = '';
  stoppingTask = false;

  // Toast
  toastMessage: string | null = null;
  toastType: 'success' | 'error' | null = null;

  // Metrics
  metrics: TaskMetric[] = [
    { label: 'Total Tasks', value: '—', icon: 'M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 010 3.75H5.625a1.875 1.875 0 010-3.75z', color: 'text-black' },
    { label: 'Running', value: '—', icon: 'M5.25 5.653c0-.856.917-1.398 1.667-.986l11.54 6.348a1.125 1.125 0 010 1.971l-11.54 6.347a1.125 1.125 0 01-1.667-.985V5.653z', color: 'text-green-600' },
    { label: 'Stopped', value: '—', icon: 'M5.25 7.5A2.25 2.25 0 017.5 5.25h9a2.25 2.25 0 012.25 2.25v9a2.25 2.25 0 01-2.25 2.25h-9a2.25 2.25 0 01-2.25-2.25v-9z', color: 'text-red-500' },
    { label: 'Pending', value: '—', icon: 'M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-yellow-600' }
  ];

  Math = Math;

  constructor(
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  private loadData(): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.loadTasks(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadTasks(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const taskReqs = accounts.map(a =>
      this.cloudServicesService.getEcsTasks(a.id).pipe(catchError(() => of({ tasks: [] })))
    );
    const clusterReqs = accounts.map(a =>
      this.cloudServicesService.getClusters(a.id).pipe(catchError(() => of({ clusters: [] })))
    );

    forkJoin([forkJoin(taskReqs), forkJoin(clusterReqs)]).subscribe(([taskResults, clusterResults]) => {
      this.tasks = taskResults.flatMap((r: any) => r.tasks || []);
      this.clusters = clusterResults.flatMap((r: any) => r.clusters || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.tasks.length.toString();
    this.metrics[1].value = this.tasks.filter(t => t.lastStatus?.toLowerCase() === 'running' || t.desiredStatus?.toLowerCase() === 'running').length.toString();
    this.metrics[2].value = this.tasks.filter(t => t.lastStatus?.toLowerCase() === 'stopped').length.toString();
    this.metrics[3].value = this.tasks.filter(t => ['pending', 'provisioning'].includes(t.lastStatus?.toLowerCase())).length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredTasks(): EcsTask[] {
    let result = [...this.tasks];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(t =>
        t.taskArn?.toLowerCase().includes(q) ||
        t.clusterName?.toLowerCase().includes(q) ||
        t.group?.toLowerCase().includes(q) ||
        t.taskDefinitionArn?.toLowerCase().includes(q) ||
        t.lastStatus?.toLowerCase().includes(q) ||
        t.stoppedReason?.toLowerCase().includes(q)
      );
    }

    if (this.selectedStatus !== 'all') {
      result = result.filter(t => t.lastStatus?.toLowerCase() === this.selectedStatus);
    }
    if (this.selectedCluster !== 'all') {
      result = result.filter(t => t.clusterName === this.selectedCluster);
    }
    if (this.selectedAccount !== 'all') {
      result = result.filter(t => t.cloudAccountId === +this.selectedAccount);
    }
    if (this.selectedLaunchType !== 'all') {
      result = result.filter(t => t.launchType?.toLowerCase() === this.selectedLaunchType.toLowerCase());
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'clusterName': valA = a.clusterName || ''; valB = b.clusterName || ''; break;
        case 'lastStatus': valA = a.lastStatus || ''; valB = b.lastStatus || ''; break;
        case 'launchType': valA = a.launchType || ''; valB = b.launchType || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        case 'stoppedAt': valA = a.stoppedAt || ''; valB = b.stoppedAt || ''; break;
        default: valA = a.createdAt || ''; valB = b.createdAt || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueStatuses(): string[] {
    return [...new Set(this.tasks.map(t => t.lastStatus?.toLowerCase()).filter(Boolean))] as string[];
  }

  get uniqueClusters(): string[] {
    return [...new Set(this.tasks.map(t => t.clusterName).filter(Boolean))].sort();
  }

  get uniqueLaunchTypes(): string[] {
    return [...new Set(this.tasks.map(t => t.launchType).filter(Boolean))].sort();
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery.trim() !== '' ||
      this.selectedStatus !== 'all' ||
      this.selectedCluster !== 'all' ||
      this.selectedAccount !== 'all' ||
      this.selectedLaunchType !== 'all';
  }

  // ── Actions ──

  toggleSort(field: SortField): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'asc';
    }
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode = mode;
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedStatus = 'all';
    this.selectedCluster = 'all';
    this.selectedAccount = 'all';
    this.selectedLaunchType = 'all';
  }

  refreshData(): void {
    this.loadData();
  }

  openDetail(task: EcsTask): void {
    this.selectedTask = task;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedTask = null, 300);
  }

  // ── Sync Tasks ──

  syncAllTasks(): void {
    if (this.syncing) return;
    this.syncing = true;

    const awsAccounts = this.accounts.filter(a => a.provider?.toUpperCase() === 'AWS');
    if (awsAccounts.length === 0) {
      this.syncing = false;
      this.showToast('No AWS accounts to sync.', 'error');
      return;
    }

    // Sync tasks for each cluster
    const clusterNames = this.uniqueClusters.length > 0 ? this.uniqueClusters : this.clusters.map(c => c.name);
    if (clusterNames.length === 0) {
      this.syncing = false;
      this.showToast('No clusters found to sync.', 'error');
      return;
    }

    const requests = awsAccounts.flatMap(a =>
      clusterNames.map(cn =>
        this.cloudServicesService.syncTasks(a.id, cn).pipe(catchError(() => of(null)))
      )
    );

    forkJoin(requests).subscribe({
      next: (results) => {
        const valid = results.filter((r): r is any => r !== null);
        const added = valid.reduce((s, r) => s + (r.added || 0), 0);
        const updated = valid.reduce((s, r) => s + (r.updated || 0), 0);
        const removed = valid.reduce((s, r) => s + (r.removed || 0), 0);
        this.syncing = false;
        this.showToast(`Tasks synced — ${added} added, ${updated} updated, ${removed} removed`, 'success');
        this.loadData();
      },
      error: () => {
        this.syncing = false;
        this.showToast('Sync failed.', 'error');
      }
    });
  }

  // ── Stop Task ──

  openStopDialog(task: EcsTask, event?: Event): void {
    if (event) event.stopPropagation();
    this.stopTarget = task;
    this.stopReason = '';
    this.showStopDialog = true;
  }

  closeStopDialog(): void {
    this.showStopDialog = false;
    this.stopTarget = null;
  }

  confirmStop(): void {
    if (!this.stopTarget || this.stoppingTask) return;
    this.stoppingTask = true;

    this.cloudServicesService.stopTask(this.stopTarget.id, this.stopTarget.cloudAccountId, { reason: this.stopReason || 'Stopped via CloudZen' }).subscribe({
      next: () => {
        this.stoppingTask = false;
        this.showStopDialog = false;
        this.showToast('Stop signal sent. Task will transition to STOPPED shortly.', 'success');
        this.loadData();
      },
      error: (err) => {
        this.stoppingTask = false;
        this.showToast(err?.error?.message || 'Failed to stop task.', 'error');
      }
    });
  }

  // ── Display Helpers ──

  getShortTaskId(taskArn: string): string {
    if (!taskArn) return '—';
    const parts = taskArn.split('/');
    return parts[parts.length - 1]?.substring(0, 12) || '—';
  }

  getFullTaskId(taskArn: string): string {
    if (!taskArn) return '—';
    const parts = taskArn.split('/');
    return parts[parts.length - 1] || '—';
  }

  getStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'connected'].includes(s)) return 'status-active';
    if (['pending', 'provisioning'].includes(s)) return 'status-pending';
    if (['stopped', 'inactive', 'failed', 'deprovisioning'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'connected'].includes(s)) return 'bg-green-500';
    if (['pending', 'provisioning'].includes(s)) return 'bg-yellow-500';
    if (['stopped', 'inactive', 'failed', 'deprovisioning'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getStatusBgClass(status: string): string {
    const s = status?.toLowerCase();
    if (['running', 'connected'].includes(s)) return 'bg-green-50 border-green-200';
    if (['pending', 'provisioning'].includes(s)) return 'bg-yellow-50 border-yellow-200';
    if (['stopped', 'failed'].includes(s)) return 'bg-red-50 border-red-200';
    return 'bg-gray-50 border-gray-200';
  }

  getStatusTextClass(status: string): string {
    const s = status?.toLowerCase();
    if (['running', 'connected'].includes(s)) return 'text-green-700';
    if (['pending', 'provisioning'].includes(s)) return 'text-yellow-700';
    if (['stopped', 'failed'].includes(s)) return 'text-red-700';
    return 'text-gray-600';
  }

  getLaunchTypeClass(type: string): string {
    switch (type?.toUpperCase()) {
      case 'FARGATE': return 'bg-blue-50 text-blue-700';
      case 'EC2': return 'bg-orange-50 text-orange-700';
      case 'EXTERNAL': return 'bg-purple-50 text-purple-700';
      default: return 'bg-gray-50 text-gray-500';
    }
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
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
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 30) return `${diffDays}d ago`;
    return this.formatDate(dateStr);
  }

  private showToast(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    setTimeout(() => this.toastMessage = null, 5000);
  }
}
