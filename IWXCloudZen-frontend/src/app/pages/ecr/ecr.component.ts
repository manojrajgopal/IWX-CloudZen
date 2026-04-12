import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../services/cloud-account.service';
import { CloudServicesService } from '../../services/cloud-services.service';
import { CloudAccount } from '../../models/cloud-account.model';
import { EcrRepository } from '../../models/cloud-services.model';
import { EcrFilterByProviderPipe } from './ecr.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'repositoryName' | 'registryId' | 'provider' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface EcrMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-ecr',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, EcrFilterByProviderPipe],
  templateUrl: './ecr.component.html',
  styleUrls: ['./ecr.component.css']
})
export class EcrComponent implements OnInit {
  accounts: CloudAccount[] = [];
  repositories: EcrRepository[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedAccount = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'repositoryName';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedRepo: EcrRepository | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: EcrMetric[] = [
    { label: 'Total Repositories', value: '—', icon: 'M21 16.5V7.5a2.25 2.25 0 00-1.133-1.957l-6.75-3.857a2.25 2.25 0 00-2.234 0l-6.75 3.857A2.25 2.25 0 003 7.5v9a2.25 2.25 0 001.133 1.957l6.75 3.857a2.25 2.25 0 002.234 0l6.75-3.857A2.25 2.25 0 0021 16.5zM12 8.25l-6.75-3.857M12 8.25v13.5m0-13.5L18.75 4.393', color: 'text-black' },
    { label: 'Registries', value: '—', icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4', color: 'text-blue-600' },
    { label: 'Providers', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-green-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS ECR' },
    { value: 'Azure', label: 'Azure ACR' },
    { value: 'GCP', label: 'GCP Artifact Registry' }
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
        this.loadRepositories(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadRepositories(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getEcrRepositories(a.id).pipe(catchError(() => of({ repositories: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.repositories = results.flatMap((r: any) => r.repositories || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.repositories.length.toString();
    const uniqueRegistries = new Set(this.repositories.map(r => r.registryId).filter(Boolean));
    this.metrics[1].value = uniqueRegistries.size.toString();
    const uniqueProviders = new Set(this.repositories.map(r => r.provider).filter(Boolean));
    this.metrics[2].value = uniqueProviders.size.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredRepositories(): EcrRepository[] {
    let result = [...this.repositories];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(r =>
        r.repositoryName?.toLowerCase().includes(q) ||
        r.repositoryUri?.toLowerCase().includes(q) ||
        r.registryId?.toLowerCase().includes(q) ||
        r.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(r => r.provider === this.selectedProvider);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(r => r.cloudAccountId === +this.selectedAccount);
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'repositoryName': valA = a.repositoryName || ''; valB = b.repositoryName || ''; break;
        case 'registryId': valA = a.registryId || ''; valB = b.registryId || ''; break;
        case 'provider': valA = a.provider || ''; valB = b.provider || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.repositoryName || ''; valB = b.repositoryName || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueRegistries(): string[] {
    return [...new Set(this.repositories.map(r => r.registryId).filter(Boolean))].sort();
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

  openDetail(repo: EcrRepository): void {
    this.selectedRepo = repo;
    this.showDetailPanel = true;
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    setTimeout(() => this.selectedRepo = null, 300);
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedAccount = 'all';
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

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS ECR';
      case 'AZURE': return 'Azure ACR';
      case 'GCP': return 'GCP Artifact Registry';
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

  truncateUri(uri: string): string {
    if (!uri) return '—';
    if (uri.length <= 50) return uri;
    return uri.substring(0, 25) + '...' + uri.substring(uri.length - 20);
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
      this.selectedAccount !== 'all';
  }
}
