import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { Ec2Instance, Ec2SyncResponse } from '../../../models/cloud-services.model';
import { Ec2FilterByProviderPipe, Ec2FilterByStatePipe, Ec2FilterByTypePipe } from './ec2-instances.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'name' | 'instanceId' | 'instanceType' | 'state' | 'launchTime' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface Ec2Metric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-ec2-instances',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, Ec2FilterByProviderPipe, Ec2FilterByStatePipe, Ec2FilterByTypePipe],
  templateUrl: './ec2-instances.component.html',
  styleUrls: ['./ec2-instances.component.css']
})
export class Ec2InstancesComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  instances: Ec2Instance[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedState = 'all';
  selectedAccount = 'all';
  selectedType = 'all';
  selectedVpc = 'all';
  selectedArchitecture = 'all';
  selectedMonitoring = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'name';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedInstance: Ec2Instance | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: Ec2Metric[] = [
    { label: 'Total Instances', value: '—', icon: 'M5.25 14.25h13.5m-13.5 0a3 3 0 01-3-3m3 3a3 3 0 100 6h13.5a3 3 0 100-6m-16.5-3a3 3 0 013-3h13.5a3 3 0 013 3m-19.5 0a4.5 4.5 0 01.9-2.7L5.737 5.1a3.375 3.375 0 012.7-1.35h7.126c1.062 0 2.062.5 2.7 1.35l2.587 3.45a4.5 4.5 0 01.9 2.7', color: 'text-black' },
    { label: 'Running', value: '—', icon: 'M5.636 5.636a9 9 0 1012.728 0M12 3v9', color: 'text-green-600' },
    { label: 'Stopped', value: '—', icon: 'M5.25 7.5A2.25 2.25 0 017.5 5.25h9a2.25 2.25 0 012.25 2.25v9a2.25 2.25 0 01-2.25 2.25h-9a2.25 2.25 0 01-2.25-2.25v-9z', color: 'text-red-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS EC2' },
    { value: 'Azure', label: 'Azure VM' },
    { value: 'GCP', label: 'GCP Compute' }
  ];

  // Sync
  syncing = false;
  lastSyncResults: Ec2SyncResponse[] = [];
  showSyncReport = false;
  syncMessage: string | null = null;
  syncMessageType: 'success' | 'error' | null = null;

  constructor(
    private router: Router,
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
        this.loadInstances(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadInstances(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getEc2Instances(a.id).pipe(catchError(() => of({ instances: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.instances = results.flatMap((r: any) => r.instances || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.instances.length.toString();
    this.metrics[1].value = this.instances.filter(i => i.state?.toLowerCase() === 'running').length.toString();
    this.metrics[2].value = this.instances.filter(i => i.state?.toLowerCase() === 'stopped').length.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredInstances(): Ec2Instance[] {
    let result = [...this.instances];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(i =>
        i.name?.toLowerCase().includes(q) ||
        i.instanceId?.toLowerCase().includes(q) ||
        i.instanceType?.toLowerCase().includes(q) ||
        i.publicIpAddress?.toLowerCase().includes(q) ||
        i.privateIpAddress?.toLowerCase().includes(q) ||
        i.vpcId?.toLowerCase().includes(q) ||
        i.keyName?.toLowerCase().includes(q) ||
        i.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') result = result.filter(i => i.provider === this.selectedProvider);
    if (this.selectedState !== 'all') result = result.filter(i => i.state?.toLowerCase() === this.selectedState);
    if (this.selectedAccount !== 'all') result = result.filter(i => i.cloudAccountId === +this.selectedAccount);
    if (this.selectedType !== 'all') result = result.filter(i => i.instanceType === this.selectedType);
    if (this.selectedVpc !== 'all') result = result.filter(i => i.vpcId === this.selectedVpc);
    if (this.selectedArchitecture !== 'all') result = result.filter(i => i.architecture === this.selectedArchitecture);
    if (this.selectedMonitoring !== 'all') result = result.filter(i => i.monitoring?.toLowerCase() === this.selectedMonitoring);

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'name': valA = a.name || a.instanceId || ''; valB = b.name || b.instanceId || ''; break;
        case 'instanceId': valA = a.instanceId || ''; valB = b.instanceId || ''; break;
        case 'instanceType': valA = a.instanceType || ''; valB = b.instanceType || ''; break;
        case 'state': valA = a.state || ''; valB = b.state || ''; break;
        case 'launchTime': valA = a.launchTime || ''; valB = b.launchTime || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.name || ''; valB = b.name || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueStates(): string[] {
    return [...new Set(this.instances.map(i => i.state?.toLowerCase()).filter(Boolean))] as string[];
  }

  get uniqueTypes(): string[] {
    return [...new Set(this.instances.map(i => i.instanceType).filter(Boolean))].sort();
  }

  get uniqueVpcs(): string[] {
    return [...new Set(this.instances.map(i => i.vpcId).filter(Boolean))].sort();
  }

  get uniqueArchitectures(): string[] {
    return [...new Set(this.instances.map(i => i.architecture).filter(Boolean))].sort();
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

  setViewMode(mode: ViewMode): void { this.viewMode = mode; }

  openDetail(inst: Ec2Instance): void {
    this.selectedInstance = inst;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedInstance = null, 300);
  }

  goToOverview(inst: Ec2Instance): void {
    this.closeDetail();
    this.router.navigate(['/dashboard/ec2-instances', inst.id]);
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedState = 'all';
    this.selectedAccount = 'all';
    this.selectedType = 'all';
    this.selectedVpc = 'all';
    this.selectedArchitecture = 'all';
    this.selectedMonitoring = 'all';
  }

  refreshData(): void { this.loadData(); }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getStateClass(state: string): string {
    const s = state?.toLowerCase();
    if (s === 'running') return 'status-running';
    if (s === 'stopped') return 'status-stopped';
    if (['pending', 'stopping', 'shutting-down'].includes(s)) return 'status-pending';
    if (s === 'terminated') return 'status-terminated';
    return 'status-unknown';
  }

  getStateDotClass(state: string): string {
    const s = state?.toLowerCase();
    if (s === 'running') return 'bg-green-500';
    if (s === 'stopped') return 'bg-red-500';
    if (['pending', 'stopping', 'shutting-down'].includes(s)) return 'bg-yellow-500';
    if (s === 'terminated') return 'bg-gray-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS EC2';
      case 'AZURE': return 'Azure VM';
      case 'GCP': return 'GCP Compute';
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

  getMonitoringClass(monitoring: string): string {
    const m = monitoring?.toLowerCase();
    if (m === 'enabled') return 'bg-green-50 text-green-700';
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
    return d.toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery.trim() !== '' ||
      this.selectedProvider !== 'all' ||
      this.selectedState !== 'all' ||
      this.selectedAccount !== 'all' ||
      this.selectedType !== 'all' ||
      this.selectedVpc !== 'all' ||
      this.selectedArchitecture !== 'all' ||
      this.selectedMonitoring !== 'all';
  }

  get runningCount(): number {
    return this.instances.filter(i => i.state?.toLowerCase() === 'running').length;
  }

  get stoppedCount(): number {
    return this.instances.filter(i => i.state?.toLowerCase() === 'stopped').length;
  }

  get withPublicIpCount(): number {
    return this.instances.filter(i => i.publicIpAddress).length;
  }

  get ebsOptimizedCount(): number {
    return this.instances.filter(i => i.ebsOptimized).length;
  }

  // ── Sync ──

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
      this.cloudServicesService.syncEc2Instances(a.id).pipe(catchError(() => of(null)))
    );

    forkJoin(syncRequests).subscribe({
      next: (results) => {
        this.syncing = false;
        this.lastSyncResults = results.filter((r): r is Ec2SyncResponse => r !== null);
        const totalAdded = this.lastSyncResults.reduce((sum, r) => sum + (r.added || 0), 0);
        const totalUpdated = this.lastSyncResults.reduce((sum, r) => sum + (r.updated || 0), 0);
        const totalInstances = this.lastSyncResults.reduce((sum, r) => sum + (r.instances?.length || 0), 0);
        this.syncMessage = `Sync completed — ${awsAccounts.length} account${awsAccounts.length !== 1 ? 's' : ''} · ${totalInstances} instance${totalInstances !== 1 ? 's' : ''} · +${totalAdded} added · ${totalUpdated} updated`;
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
}
