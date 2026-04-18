import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { Vpc, VpcSyncResponse } from '../../../models/cloud-services.model';
import { VpcFilterByProviderPipe, VpcFilterByStatePipe } from './vpcs.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'name' | 'vpcId' | 'state' | 'cidrBlock' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface VpcMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-vpcs',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, VpcFilterByProviderPipe, VpcFilterByStatePipe],
  templateUrl: './vpcs.component.html',
  styleUrls: ['./vpcs.component.css']
})
export class VpcsComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  vpcs: Vpc[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedState = 'all';
  selectedAccount = 'all';
  selectedDefault = 'all';
  selectedDns = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'name';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedVpc: Vpc | null = null;
  showDetailPanel = false;

  // Sync state
  syncing = false;
  lastSyncResults: VpcSyncResponse[] = [];
  showSyncReport = false;
  syncMessage: string | null = null;
  syncMessageType: 'success' | 'error' | null = null;

  // Metrics
  metrics: VpcMetric[] = [
    { label: 'Total VPCs', value: '—', icon: 'M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-black' },
    { label: 'Available', value: '—', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Default VPCs', value: '—', icon: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z', color: 'text-yellow-500' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS VPC' },
    { value: 'Azure', label: 'Azure VNet' },
    { value: 'GCP', label: 'GCP VPC' }
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
        this.loadVpcs(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadVpcs(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getVpcs(a.id).pipe(catchError(() => of({ vpcs: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.vpcs = results.flatMap((r: any) => r.vpcs || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.vpcs.length.toString();
    this.metrics[1].value = this.vpcs.filter(v => v.state?.toLowerCase() === 'available').length.toString();
    this.metrics[2].value = this.vpcs.filter(v => v.isDefault).length.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredVpcs(): Vpc[] {
    let result = [...this.vpcs];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(v =>
        v.name?.toLowerCase().includes(q) ||
        v.vpcId?.toLowerCase().includes(q) ||
        v.cidrBlock?.toLowerCase().includes(q) ||
        v.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(v => v.provider === this.selectedProvider);
    }

    if (this.selectedState !== 'all') {
      result = result.filter(v => v.state?.toLowerCase() === this.selectedState);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(v => v.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedDefault !== 'all') {
      const isDefault = this.selectedDefault === 'yes';
      result = result.filter(v => v.isDefault === isDefault);
    }

    if (this.selectedDns !== 'all') {
      if (this.selectedDns === 'support') {
        result = result.filter(v => v.enableDnsSupport);
      } else if (this.selectedDns === 'hostnames') {
        result = result.filter(v => v.enableDnsHostnames);
      } else if (this.selectedDns === 'both') {
        result = result.filter(v => v.enableDnsSupport && v.enableDnsHostnames);
      }
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'name': valA = a.name || a.vpcId; valB = b.name || b.vpcId; break;
        case 'vpcId': valA = a.vpcId || ''; valB = b.vpcId || ''; break;
        case 'state': valA = a.state || ''; valB = b.state || ''; break;
        case 'cidrBlock': valA = a.cidrBlock || ''; valB = b.cidrBlock || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.name || ''; valB = b.name || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueStates(): string[] {
    return [...new Set(this.vpcs.map(v => v.state?.toLowerCase()).filter(Boolean))] as string[];
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

  openDetail(vpc: Vpc): void {
    this.selectedVpc = vpc;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedVpc = null, 300);
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedState = 'all';
    this.selectedAccount = 'all';
    this.selectedDefault = 'all';
    this.selectedDns = 'all';
  }

  refreshData(): void {
    this.loadData();
  }

  syncData(): void {
    if (this.syncing) return;
    this.syncing = true;
    this.syncMessage = null;
    this.lastSyncResults = [];
    this.showSyncReport = false;

    const awsAccounts = this.accounts.filter(a => a.provider?.toUpperCase() === 'AWS');
    if (awsAccounts.length === 0) {
      this.syncing = false;
      this.syncMessage = 'No AWS accounts found to sync.';
      this.syncMessageType = 'error';
      setTimeout(() => { this.syncMessage = null; this.syncMessageType = null; }, 4000);
      return;
    }

    const syncRequests = awsAccounts.map(a =>
      this.cloudServicesService.syncVpcs(a.id).pipe(catchError(() => of(null)))
    );

    forkJoin(syncRequests).subscribe({
      next: (results) => {
        this.syncing = false;
        this.lastSyncResults = results.filter((r): r is VpcSyncResponse => r !== null);

        const totalAdded = this.getTotalSyncAdded();
        const totalVpcs = this.getTotalSyncVpcs();

        this.syncMessage = `Sync completed — ${awsAccounts.length} account${awsAccounts.length !== 1 ? 's' : ''} · ${totalVpcs} VPC${totalVpcs !== 1 ? 's' : ''} · +${totalAdded} added`;
        this.syncMessageType = 'success';
        this.showSyncReport = true;
        setTimeout(() => { this.syncMessage = null; this.syncMessageType = null; }, 6000);
        this.loadData();
      },
      error: () => {
        this.syncing = false;
        this.syncMessage = 'Sync failed. Please try again.';
        this.syncMessageType = 'error';
        setTimeout(() => { this.syncMessage = null; this.syncMessageType = null; }, 4000);
      }
    });
  }

  closeSyncReport(): void {
    this.showSyncReport = false;
  }

  getTotalSyncAdded(): number {
    return this.lastSyncResults.reduce((sum, r) => sum + (r.added || 0), 0);
  }

  getTotalSyncUpdated(): number {
    return this.lastSyncResults.reduce((sum, r) => sum + (r.updated || 0), 0);
  }

  getTotalSyncRemoved(): number {
    return this.lastSyncResults.reduce((sum, r) => sum + (r.removed || 0), 0);
  }

  getTotalSyncVpcs(): number {
    return this.lastSyncResults.reduce((sum, r) => sum + (r.vpcs?.length || 0), 0);
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getAccountProvider(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.provider : 'Unknown';
  }

  getDisplayName(vpc: Vpc): string {
    return vpc.name || vpc.vpcId || '—';
  }

  getStatusClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'active'].includes(s)) return 'status-active';
    if (['pending', 'creating', 'updating'].includes(s)) return 'status-pending';
    if (['deleted', 'terminated', 'inactive', 'error'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'active'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-500';
    if (['deleted', 'terminated', 'inactive', 'error'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS VPC';
      case 'AZURE': return 'Azure VNet';
      case 'GCP': return 'GCP VPC';
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
      this.selectedState !== 'all' ||
      this.selectedAccount !== 'all' ||
      this.selectedDefault !== 'all' ||
      this.selectedDns !== 'all';
  }
}
