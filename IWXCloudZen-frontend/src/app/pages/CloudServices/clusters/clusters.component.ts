import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { Cluster } from '../../../models/cloud-services.model';
import { ClusterFilterByProviderPipe, ClusterFilterByStatusPipe } from './clusters.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'name' | 'provider' | 'status' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface ClusterMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-clusters',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ClusterFilterByProviderPipe, ClusterFilterByStatusPipe],
  templateUrl: './clusters.component.html',
  styleUrls: ['./clusters.component.css']
})
export class ClustersComponent implements OnInit {
  // Data
  accounts: CloudAccount[] = [];
  clusters: Cluster[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedStatus = 'all';
  selectedAccount = 'all';
  selectedInsights = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'name';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedCluster: Cluster | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: ClusterMetric[] = [
    { label: 'Total Clusters', value: '—', icon: 'M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z', color: 'text-black' },
    { label: 'Active', value: '—', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Insights Enabled', value: '—', icon: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z', color: 'text-blue-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  // Provider options
  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS ECS/EKS' },
    { value: 'Azure', label: 'Azure AKS' },
    { value: 'GCP', label: 'GCP GKE' }
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
        this.loadClusters(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadClusters(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getClusters(a.id).pipe(catchError(() => of({ clusters: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.clusters = results.flatMap((r: any) => r.clusters || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.clusters.length.toString();
    this.metrics[1].value = this.clusters.filter(c => c.status?.toLowerCase() === 'active' || c.status?.toLowerCase() === 'running').length.toString();
    this.metrics[2].value = this.clusters.filter(c => c.containerInsightsEnabled).length.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredClusters(): Cluster[] {
    let result = [...this.clusters];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(c =>
        c.name.toLowerCase().includes(q) ||
        c.provider?.toLowerCase().includes(q) ||
        c.clusterArn?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(c => c.provider === this.selectedProvider);
    }

    if (this.selectedStatus !== 'all') {
      result = result.filter(c => c.status?.toLowerCase() === this.selectedStatus);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(c => c.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedInsights !== 'all') {
      const insightsVal = this.selectedInsights === 'enabled';
      result = result.filter(c => c.containerInsightsEnabled === insightsVal);
    }

    // Sort
    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'name': valA = a.name; valB = b.name; break;
        case 'provider': valA = a.provider || ''; valB = b.provider || ''; break;
        case 'status': valA = a.status || ''; valB = b.status || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.name; valB = b.name;
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueStatuses(): string[] {
    return [...new Set(this.clusters.map(c => c.status?.toLowerCase()).filter(Boolean))] as string[];
  }

  get uniqueProviders(): string[] {
    return [...new Set(this.clusters.map(c => c.provider).filter(Boolean))].sort();
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

  openDetail(cluster: Cluster): void {
    this.selectedCluster = cluster;
    this.showDetailPanel = true;
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    setTimeout(() => this.selectedCluster = null, 300);
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedStatus = 'all';
    this.selectedAccount = 'all';
    this.selectedInsights = 'all';
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
    if (['active', 'running', 'available'].includes(s)) return 'status-active';
    if (['pending', 'creating', 'updating', 'provisioning'].includes(s)) return 'status-pending';
    if (['deleted', 'terminated', 'inactive', 'error', 'failed'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'available'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating', 'provisioning'].includes(s)) return 'bg-yellow-500';
    if (['deleted', 'terminated', 'inactive', 'error', 'failed'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS ECS/EKS';
      case 'AZURE': return 'Azure AKS';
      case 'GCP': return 'GCP GKE';
      default: return provider || 'Unknown';
    }
  }

  getProviderIcon(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'M15.75 10.5l4.72-4.72a.75.75 0 011.28.53v11.38a.75.75 0 01-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 002.25-2.25v-9a2.25 2.25 0 00-2.25-2.25h-9A2.25 2.25 0 002.25 7.5v9a2.25 2.25 0 002.25 2.25z';
      case 'AZURE': return 'M2.25 15a4.5 4.5 0 004.5 4.5H18a3.75 3.75 0 001.332-7.257 3 3 0 00-3.758-3.848 5.25 5.25 0 00-10.233 2.33A4.502 4.502 0 002.25 15z';
      case 'GCP': return 'M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582m15.686 0A11.953 11.953 0 0112 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0121 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0112 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 013 12c0-1.605.42-3.113 1.157-4.418';
      default: return 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z';
    }
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

  truncateArn(arn: string | null): string {
    if (!arn) return '—';
    if (arn.length <= 40) return arn;
    return arn.substring(0, 20) + '...' + arn.substring(arn.length - 16);
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery.trim() !== '' ||
      this.selectedProvider !== 'all' ||
      this.selectedStatus !== 'all' ||
      this.selectedAccount !== 'all' ||
      this.selectedInsights !== 'all';
  }
}
