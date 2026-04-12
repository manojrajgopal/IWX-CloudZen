import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { Subnet } from '../../../models/cloud-services.model';
import { SubnetFilterByProviderPipe, SubnetFilterByStatePipe, SubnetFilterByAzPipe } from './subnets.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'name' | 'subnetId' | 'vpcId' | 'availabilityZone' | 'state' | 'cidrBlock' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface SubnetMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-subnets',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, SubnetFilterByProviderPipe, SubnetFilterByStatePipe, SubnetFilterByAzPipe],
  templateUrl: './subnets.component.html',
  styleUrls: ['./subnets.component.css']
})
export class SubnetsComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  subnets: Subnet[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedState = 'all';
  selectedAccount = 'all';
  selectedVpc = 'all';
  selectedAz = 'all';
  selectedDefault = 'all';
  selectedPublicIp = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'name';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedSubnet: Subnet | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: SubnetMetric[] = [
    { label: 'Total Subnets', value: '—', icon: 'M3.75 6A2.25 2.25 0 016 3.75h2.25A2.25 2.25 0 0110.5 6v2.25a2.25 2.25 0 01-2.25 2.25H6a2.25 2.25 0 01-2.25-2.25V6zm0 9.75A2.25 2.25 0 016 13.5h2.25a2.25 2.25 0 012.25 2.25V18a2.25 2.25 0 01-2.25 2.25H6A2.25 2.25 0 013.75 18v-2.25zM13.5 6a2.25 2.25 0 012.25-2.25H18A2.25 2.25 0 0120.25 6v2.25A2.25 2.25 0 0118 10.5h-2.25a2.25 2.25 0 01-2.25-2.25V6zm0 9.75a2.25 2.25 0 012.25-2.25H18a2.25 2.25 0 012.25 2.25V18A2.25 2.25 0 0118 20.25h-2.25A2.25 2.25 0 0113.5 18v-2.25z', color: 'text-black' },
    { label: 'Available', value: '—', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Available IPs', value: '—', icon: 'M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m16.5 5.507A8.973 8.973 0 0012 3', color: 'text-blue-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS VPC' },
    { value: 'Azure', label: 'Azure VNet' },
    { value: 'GCP', label: 'GCP Subnet' }
  ];

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
        this.loadSubnets(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadSubnets(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getSubnets(a.id).pipe(catchError(() => of({ totalCount: 0, vpcIdFilter: null, subnets: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.subnets = results.flatMap((r: any) => r.subnets || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.subnets.length.toString();
    this.metrics[1].value = this.subnets.filter(s => s.state?.toLowerCase() === 'available').length.toString();
    this.metrics[2].value = this.subnets.reduce((sum, s) => sum + (s.availableIpAddressCount || 0), 0).toLocaleString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredSubnets(): Subnet[] {
    let result = [...this.subnets];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(s =>
        s.name?.toLowerCase().includes(q) ||
        s.subnetId?.toLowerCase().includes(q) ||
        s.vpcId?.toLowerCase().includes(q) ||
        s.cidrBlock?.toLowerCase().includes(q) ||
        s.availabilityZone?.toLowerCase().includes(q) ||
        s.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(s => s.provider === this.selectedProvider);
    }

    if (this.selectedState !== 'all') {
      result = result.filter(s => s.state?.toLowerCase() === this.selectedState);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(s => s.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedVpc !== 'all') {
      result = result.filter(s => s.vpcId === this.selectedVpc);
    }

    if (this.selectedAz !== 'all') {
      result = result.filter(s => s.availabilityZone === this.selectedAz);
    }

    if (this.selectedDefault !== 'all') {
      const isDefault = this.selectedDefault === 'yes';
      result = result.filter(s => s.isDefault === isDefault);
    }

    if (this.selectedPublicIp !== 'all') {
      const mapPublic = this.selectedPublicIp === 'yes';
      result = result.filter(s => s.mapPublicIpOnLaunch === mapPublic);
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'name': valA = a.name || a.subnetId || ''; valB = b.name || b.subnetId || ''; break;
        case 'subnetId': valA = a.subnetId || ''; valB = b.subnetId || ''; break;
        case 'vpcId': valA = a.vpcId || ''; valB = b.vpcId || ''; break;
        case 'availabilityZone': valA = a.availabilityZone || ''; valB = b.availabilityZone || ''; break;
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
    return [...new Set(this.subnets.map(s => s.state?.toLowerCase()).filter(Boolean))] as string[];
  }

  get uniqueVpcs(): string[] {
    return [...new Set(this.subnets.map(s => s.vpcId).filter(Boolean))].sort();
  }

  get uniqueAzs(): string[] {
    return [...new Set(this.subnets.map(s => s.availabilityZone).filter(Boolean))].sort();
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

  openDetail(subnet: Subnet): void {
    this.selectedSubnet = subnet;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedSubnet = null, 300);
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedState = 'all';
    this.selectedAccount = 'all';
    this.selectedVpc = 'all';
    this.selectedAz = 'all';
    this.selectedDefault = 'all';
    this.selectedPublicIp = 'all';
  }

  refreshData(): void {
    this.loadData();
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getStateClass(state: string): string {
    const s = state?.toLowerCase();
    if (s === 'available') return 'status-available';
    if (s === 'pending') return 'status-pending';
    return 'status-unknown';
  }

  getStateDotClass(state: string): string {
    const s = state?.toLowerCase();
    if (s === 'available') return 'bg-green-500';
    if (s === 'pending') return 'bg-yellow-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS VPC';
      case 'AZURE': return 'Azure VNet';
      case 'GCP': return 'GCP Subnet';
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

  getBooleanLabel(val: boolean): string {
    return val ? 'Yes' : 'No';
  }

  getBooleanClass(val: boolean): string {
    return val ? 'bg-green-50 text-green-700' : 'bg-gray-50 text-gray-500';
  }

  getIpUtilization(subnet: Subnet): string {
    // We don't have total IPs from the model, so just show available count
    return subnet.availableIpAddressCount?.toLocaleString() || '0';
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
      this.selectedVpc !== 'all' ||
      this.selectedAz !== 'all' ||
      this.selectedDefault !== 'all' ||
      this.selectedPublicIp !== 'all';
  }

  get totalAvailableIps(): number {
    return this.subnets.reduce((sum, s) => sum + (s.availableIpAddressCount || 0), 0);
  }

  get publicSubnetsCount(): number {
    return this.subnets.filter(s => s.mapPublicIpOnLaunch).length;
  }

  get privateSubnetsCount(): number {
    return this.subnets.filter(s => !s.mapPublicIpOnLaunch).length;
  }
}
