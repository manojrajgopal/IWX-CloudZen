import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../services/cloud-account.service';
import { CloudServicesService } from '../../services/cloud-services.service';
import { CloudAccount } from '../../models/cloud-account.model';
import { EcsService } from '../../models/cloud-services.model';
import { EcsFilterByProviderPipe, EcsFilterByStatusPipe, EcsFilterByLaunchTypePipe } from './ecs.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'serviceName' | 'clusterName' | 'status' | 'launchType' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface EcsMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-ecs',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, EcsFilterByProviderPipe, EcsFilterByStatusPipe, EcsFilterByLaunchTypePipe],
  templateUrl: './ecs.component.html',
  styleUrls: ['./ecs.component.css']
})
export class EcsComponent implements OnInit {
  accounts: CloudAccount[] = [];
  services: EcsService[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedStatus = 'all';
  selectedAccount = 'all';
  selectedLaunchType = 'all';
  selectedCluster = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'serviceName';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedService: EcsService | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: EcsMetric[] = [
    { label: 'Total Services', value: '—', icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10', color: 'text-black' },
    { label: 'Active', value: '—', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Running Tasks', value: '—', icon: 'M13 10V3L4 14h7v7l9-11h-7z', color: 'text-blue-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS ECS' },
    { value: 'Azure', label: 'Azure Container' },
    { value: 'GCP', label: 'GCP Cloud Run' }
  ];

  Math = Math;

  constructor(
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.loadServices(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadServices(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getEcsServices(a.id).pipe(catchError(() => of({ services: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.services = results.flatMap((r: any) => r.services || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.services.length.toString();
    this.metrics[1].value = this.services.filter(s => s.status?.toLowerCase() === 'active').length.toString();
    this.metrics[2].value = this.services.reduce((sum, s) => sum + (s.runningCount || 0), 0).toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredServices(): EcsService[] {
    let result = [...this.services];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(s =>
        s.serviceName?.toLowerCase().includes(q) ||
        s.clusterName?.toLowerCase().includes(q) ||
        s.taskDefinition?.toLowerCase().includes(q) ||
        s.launchType?.toLowerCase().includes(q) ||
        s.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(s => s.provider === this.selectedProvider);
    }

    if (this.selectedStatus !== 'all') {
      result = result.filter(s => s.status?.toLowerCase() === this.selectedStatus);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(s => s.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedLaunchType !== 'all') {
      result = result.filter(s => s.launchType?.toLowerCase() === this.selectedLaunchType);
    }

    if (this.selectedCluster !== 'all') {
      result = result.filter(s => s.clusterName === this.selectedCluster);
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'serviceName': valA = a.serviceName || ''; valB = b.serviceName || ''; break;
        case 'clusterName': valA = a.clusterName || ''; valB = b.clusterName || ''; break;
        case 'status': valA = a.status || ''; valB = b.status || ''; break;
        case 'launchType': valA = a.launchType || ''; valB = b.launchType || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.serviceName || ''; valB = b.serviceName || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueStatuses(): string[] {
    return [...new Set(this.services.map(s => s.status?.toLowerCase()).filter(Boolean))] as string[];
  }

  get uniqueLaunchTypes(): string[] {
    return [...new Set(this.services.map(s => s.launchType).filter(Boolean))].sort();
  }

  get uniqueClusterNames(): string[] {
    return [...new Set(this.services.map(s => s.clusterName).filter(Boolean))].sort();
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

  openDetail(svc: EcsService): void {
    this.selectedService = svc;
    this.showDetailPanel = true;
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    setTimeout(() => this.selectedService = null, 300);
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedStatus = 'all';
    this.selectedAccount = 'all';
    this.selectedLaunchType = 'all';
    this.selectedCluster = 'all';
  }

  refreshData(): void {
    this.loadData();
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getAccountProvider(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.provider : 'Unknown';
  }

  getStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running'].includes(s)) return 'status-active';
    if (['pending', 'draining', 'updating'].includes(s)) return 'status-pending';
    if (['inactive', 'stopped', 'failed'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running'].includes(s)) return 'bg-green-500';
    if (['pending', 'draining', 'updating'].includes(s)) return 'bg-yellow-500';
    if (['inactive', 'stopped', 'failed'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS ECS';
      case 'AZURE': return 'Azure Container';
      case 'GCP': return 'GCP Cloud Run';
      default: return provider || 'Unknown';
    }
  }

  getProviderIcon(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'M15.75 10.5l4.72-4.72a.75.75 0 011.28.53v11.38a.75.75 0 01-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 002.25-2.25v-9a2.25 2.25 0 00-2.25-2.25h-9A2.25 2.25 0 002.25 7.5v9a2.25 2.25 0 002.25 2.25z';
      case 'AZURE': return 'M2.25 15a4.5 4.5 0 004.5 4.5H18a3.75 3.75 0 001.332-7.257 3 3 0 00-3.758-3.848 5.25 5.25 0 00-10.233 2.33A4.502 4.502 0 002.25 15z';
      case 'GCP': return 'M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3';
      default: return 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z';
    }
  }

  getLaunchTypeClass(type: string): string {
    switch (type?.toUpperCase()) {
      case 'FARGATE': return 'bg-blue-50 text-blue-700';
      case 'EC2': return 'bg-orange-50 text-orange-700';
      case 'EXTERNAL': return 'bg-purple-50 text-purple-700';
      default: return 'bg-gray-50 text-gray-500';
    }
  }

  getTaskHealth(svc: EcsService): number {
    if (!svc.desiredCount || svc.desiredCount === 0) return 100;
    return Math.round((svc.runningCount / svc.desiredCount) * 100);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery.trim() !== '' ||
      this.selectedProvider !== 'all' ||
      this.selectedStatus !== 'all' ||
      this.selectedAccount !== 'all' ||
      this.selectedLaunchType !== 'all' ||
      this.selectedCluster !== 'all';
  }

  get totalDesired(): number {
    return this.services.reduce((sum, s) => sum + (s.desiredCount || 0), 0);
  }

  get totalRunning(): number {
    return this.services.reduce((sum, s) => sum + (s.runningCount || 0), 0);
  }

  get totalPending(): number {
    return this.services.reduce((sum, s) => sum + (s.pendingCount || 0), 0);
  }
}
