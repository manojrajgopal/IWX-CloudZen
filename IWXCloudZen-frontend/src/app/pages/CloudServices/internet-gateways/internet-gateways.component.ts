import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { InternetGateway, InternetGatewaySyncResponse, Vpc } from '../../../models/cloud-services.model';
import { IgwFilterByProviderPipe, IgwFilterByStatePipe } from './internet-gateways.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'name' | 'internetGatewayId' | 'state' | 'attachedVpcId' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface IgwMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-internet-gateways',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, IgwFilterByProviderPipe, IgwFilterByStatePipe],
  templateUrl: './internet-gateways.component.html',
  styleUrls: ['./internet-gateways.component.css']
})
export class InternetGatewaysComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  internetGateways: InternetGateway[] = [];
  vpcs: Vpc[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedState = 'all';
  selectedAccount = 'all';
  selectedAttachment = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'name';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedIgw: InternetGateway | null = null;
  showDetailPanel = false;

  // Sync state
  syncing = false;
  lastSyncResults: InternetGatewaySyncResponse[] = [];
  showSyncReport = false;
  syncMessage: string | null = null;
  syncMessageType: 'success' | 'error' | null = null;

  // Metrics
  metrics: IgwMetric[] = [
    { label: 'Total Gateways', value: '—', icon: 'M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582', color: 'text-black' },
    { label: 'Available', value: '—', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Detached', value: '—', icon: 'M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21', color: 'text-yellow-500' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS' }
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
        this.loadInternetGateways(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadInternetGateways(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const igwRequests = accounts.map(a =>
      this.cloudServicesService.getInternetGateways(a.id).pipe(catchError(() => of({ internetGateways: [] })))
    );
    const vpcRequests = accounts.map(a =>
      this.cloudServicesService.getVpcs(a.id).pipe(catchError(() => of({ vpcs: [] })))
    );

    forkJoin([...igwRequests, ...vpcRequests]).subscribe(results => {
      const igwResults = results.slice(0, accounts.length);
      const vpcResults = results.slice(accounts.length);
      this.internetGateways = igwResults.flatMap((r: any) => r.internetGateways || []);
      this.vpcs = vpcResults.flatMap((r: any) => r.vpcs || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.internetGateways.length.toString();
    this.metrics[1].value = this.internetGateways.filter(i => i.state?.toLowerCase() === 'available').length.toString();
    this.metrics[2].value = this.internetGateways.filter(i => !i.attachedVpcId || i.state?.toLowerCase() === 'detached').length.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredGateways(): InternetGateway[] {
    let result = [...this.internetGateways];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.name?.toLowerCase().includes(q) ||
        i.internetGatewayId?.toLowerCase().includes(q) ||
        i.attachedVpcId?.toLowerCase().includes(q) ||
        i.ownerId?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(i => i.provider === this.selectedProvider);
    }

    if (this.selectedState !== 'all') {
      result = result.filter(i => i.state?.toLowerCase() === this.selectedState);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(i => i.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedAttachment !== 'all') {
      if (this.selectedAttachment === 'attached') {
        result = result.filter(i => !!i.attachedVpcId && i.state?.toLowerCase() !== 'detached');
      } else {
        result = result.filter(i => !i.attachedVpcId || i.state?.toLowerCase() === 'detached');
      }
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'name': valA = a.name || a.internetGatewayId; valB = b.name || b.internetGatewayId; break;
        case 'internetGatewayId': valA = a.internetGatewayId || ''; valB = b.internetGatewayId || ''; break;
        case 'state': valA = a.state || ''; valB = b.state || ''; break;
        case 'attachedVpcId': valA = a.attachedVpcId || ''; valB = b.attachedVpcId || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.name || ''; valB = b.name || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueStates(): string[] {
    return [...new Set(this.internetGateways.map(i => i.state?.toLowerCase()).filter(Boolean))] as string[];
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

  openDetail(igw: InternetGateway): void {
    this.selectedIgw = igw;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedIgw = null, 300);
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedState = 'all';
    this.selectedAccount = 'all';
    this.selectedAttachment = 'all';
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
      this.cloudServicesService.syncInternetGateways(a.id).pipe(catchError(() => of(null)))
    );

    forkJoin(syncRequests).subscribe({
      next: (results) => {
        this.syncing = false;
        this.lastSyncResults = results.filter((r): r is InternetGatewaySyncResponse => r !== null);

        const totalAdded = this.getTotalSyncAdded();
        const totalIgws = this.getTotalSyncIgws();

        this.syncMessage = `Sync completed — ${awsAccounts.length} account${awsAccounts.length !== 1 ? 's' : ''} · ${totalIgws} gateway${totalIgws !== 1 ? 's' : ''} · +${totalAdded} added`;
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

  getTotalSyncIgws(): number {
    return this.lastSyncResults.reduce((sum, r) => sum + (r.internetGateways?.length || 0), 0);
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getAccountProvider(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.provider : 'Unknown';
  }

  getDisplayName(igw: InternetGateway): string {
    return igw.name || igw.internetGatewayId || '—';
  }

  getVpcName(vpcId: string | null): string {
    if (!vpcId) return '—';
    const vpc = this.vpcs.find(v => v.vpcId === vpcId);
    return vpc?.name || vpcId;
  }

  getStatusClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'attached'].includes(s)) return 'status-active';
    if (['pending', 'attaching', 'detaching'].includes(s)) return 'status-pending';
    if (['detached', 'deleted', 'error'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'attached'].includes(s)) return 'bg-green-500';
    if (['pending', 'attaching', 'detaching'].includes(s)) return 'bg-yellow-500';
    if (['detached', 'deleted', 'error'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS';
      default: return provider || 'Unknown';
    }
  }

  getProviderIcon(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'M15.75 10.5l4.72-4.72a.75.75 0 011.28.53v11.38a.75.75 0 01-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 002.25-2.25v-9a2.25 2.25 0 00-2.25-2.25h-9A2.25 2.25 0 002.25 7.5v9a2.25 2.25 0 002.25 2.25z';
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
      this.selectedAttachment !== 'all';
  }
}
