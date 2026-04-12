import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../services/cloud-account.service';
import { CloudServicesService } from '../../services/cloud-services.service';
import { CloudAccount } from '../../models/cloud-account.model';
import { S3Bucket } from '../../models/cloud-services.model';
import { FilterByProviderPipe, FilterByRegionPipe, FilterByStatusPipe } from './cloud-storage.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'name' | 'region' | 'status' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface StorageMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-cloud-storage',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, FilterByProviderPipe, FilterByRegionPipe, FilterByStatusPipe],
  templateUrl: './cloud-storage.component.html',
  styleUrls: ['./cloud-storage.component.css']
})
export class CloudStorageComponent implements OnInit {
  // Data
  accounts: CloudAccount[] = [];
  buckets: S3Bucket[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedRegion = 'all';
  selectedStatus = 'all';
  selectedAccount = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'name';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedBucket: S3Bucket | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: StorageMetric[] = [
    { label: 'Total Buckets', value: '—', icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4', color: 'text-black' },
    { label: 'Active', value: '—', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Regions', value: '—', icon: 'M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-blue-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  // Provider options
  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS S3' },
    { value: 'Azure', label: 'Azure Blob' },
    { value: 'GCP', label: 'GCP Storage' }
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
        this.loadBuckets(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadBuckets(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getS3Buckets(a.id).pipe(catchError(() => of({ buckets: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.buckets = results.flatMap((r: any) => r.buckets || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.buckets.length.toString();
    this.metrics[1].value = this.buckets.filter(b => b.status?.toLowerCase() === 'created' || b.status?.toLowerCase() === 'active').length.toString();
    const uniqueRegions = new Set(this.buckets.map(b => b.region));
    this.metrics[2].value = uniqueRegions.size.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredBuckets(): S3Bucket[] {
    let result = [...this.buckets];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(b =>
        b.name.toLowerCase().includes(q) ||
        b.region?.toLowerCase().includes(q) ||
        b.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(b => b.provider === this.selectedProvider);
    }

    if (this.selectedRegion !== 'all') {
      result = result.filter(b => b.region === this.selectedRegion);
    }

    if (this.selectedStatus !== 'all') {
      result = result.filter(b => b.status?.toLowerCase() === this.selectedStatus);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(b => b.cloudAccountId === +this.selectedAccount);
    }

    // Sort
    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'name': valA = a.name; valB = b.name; break;
        case 'region': valA = a.region || ''; valB = b.region || ''; break;
        case 'status': valA = a.status || ''; valB = b.status || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.name; valB = b.name;
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueRegions(): string[] {
    return [...new Set(this.buckets.map(b => b.region).filter(Boolean))].sort();
  }

  get uniqueStatuses(): string[] {
    return [...new Set(this.buckets.map(b => b.status?.toLowerCase()).filter(Boolean))] as string[];
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

  openDetail(bucket: S3Bucket): void {
    this.selectedBucket = bucket;
    this.showDetailPanel = true;
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    setTimeout(() => this.selectedBucket = null, 300);
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedRegion = 'all';
    this.selectedStatus = 'all';
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

  getStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'created', 'available'].includes(s)) return 'status-active';
    if (['pending', 'creating', 'updating'].includes(s)) return 'status-pending';
    if (['deleted', 'terminated', 'inactive', 'error'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'created', 'available'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-500';
    if (['deleted', 'terminated', 'inactive', 'error'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderIcon(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'M15.75 10.5l4.72-4.72a.75.75 0 011.28.53v11.38a.75.75 0 01-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 002.25-2.25v-9a2.25 2.25 0 00-2.25-2.25h-9A2.25 2.25 0 002.25 7.5v9a2.25 2.25 0 002.25 2.25z';
      case 'AZURE': return 'M2.25 15a4.5 4.5 0 004.5 4.5H18a3.75 3.75 0 001.332-7.257 3 3 0 00-3.758-3.848 5.25 5.25 0 00-10.233 2.33A4.502 4.502 0 002.25 15z';
      case 'GCP': return 'M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582m15.686 0A11.953 11.953 0 0112 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0121 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0112 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 013 12c0-1.605.42-3.113 1.157-4.418';
      default: return 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z';
    }
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS S3';
      case 'AZURE': return 'Azure Blob';
      case 'GCP': return 'GCP Storage';
      default: return provider || 'Unknown';
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

  get hasActiveFilters(): boolean {
    return this.searchQuery.trim() !== '' ||
      this.selectedProvider !== 'all' ||
      this.selectedRegion !== 'all' ||
      this.selectedStatus !== 'all' ||
      this.selectedAccount !== 'all';
  }
}
