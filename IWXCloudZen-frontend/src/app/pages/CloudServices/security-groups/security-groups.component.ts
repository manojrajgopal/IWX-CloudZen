import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { SecurityGroup, SecurityGroupRule, SecurityGroupSyncResponse } from '../../../models/cloud-services.model';
import { SgFilterByProviderPipe } from './security-groups.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'groupName' | 'securityGroupId' | 'vpcId' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface SgMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-security-groups',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, SgFilterByProviderPipe],
  templateUrl: './security-groups.component.html',
  styleUrls: ['./security-groups.component.css']
})
export class SecurityGroupsComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  securityGroups: SecurityGroup[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedAccount = 'all';
  selectedVpc = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'groupName';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedGroup: SecurityGroup | null = null;
  showDetailPanel = false;
  activeRulesTab: 'inbound' | 'outbound' = 'inbound';

  // Sync state
  syncing = false;
  lastSyncResults: SecurityGroupSyncResponse[] = [];
  showSyncReport = false;
  syncMessage: string | null = null;
  syncMessageType: 'success' | 'error' | null = null;

  // Metrics
  metrics: SgMetric[] = [
    { label: 'Total Groups', value: '—', icon: 'M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z', color: 'text-black' },
    { label: 'Inbound Rules', value: '—', icon: 'M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3', color: 'text-blue-600' },
    { label: 'Outbound Rules', value: '—', icon: 'M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5', color: 'text-orange-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS' },
    { value: 'Azure', label: 'Azure NSG' },
    { value: 'GCP', label: 'GCP Firewall' }
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
        this.loadSecurityGroups(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadSecurityGroups(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getSecurityGroups(a.id).pipe(catchError(() => of({ totalCount: 0, vpcIdFilter: null, securityGroups: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.securityGroups = results.flatMap((r: any) => r.securityGroups || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.securityGroups.length.toString();
    this.metrics[1].value = this.securityGroups.reduce((sum, sg) => sum + (sg.inboundRules?.length || 0), 0).toString();
    this.metrics[2].value = this.securityGroups.reduce((sum, sg) => sum + (sg.outboundRules?.length || 0), 0).toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredGroups(): SecurityGroup[] {
    let result = [...this.securityGroups];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(sg =>
        sg.groupName?.toLowerCase().includes(q) ||
        sg.securityGroupId?.toLowerCase().includes(q) ||
        sg.description?.toLowerCase().includes(q) ||
        sg.vpcId?.toLowerCase().includes(q) ||
        sg.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(sg => sg.provider === this.selectedProvider);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(sg => sg.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedVpc !== 'all') {
      result = result.filter(sg => sg.vpcId === this.selectedVpc);
    }

    result.sort((a, b) => {
      let valA: string, valB: string;
      switch (this.sortField) {
        case 'groupName': valA = a.groupName || ''; valB = b.groupName || ''; break;
        case 'securityGroupId': valA = a.securityGroupId || ''; valB = b.securityGroupId || ''; break;
        case 'vpcId': valA = a.vpcId || ''; valB = b.vpcId || ''; break;
        case 'createdAt': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.groupName || ''; valB = b.groupName || '';
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueVpcs(): string[] {
    return [...new Set(this.securityGroups.map(sg => sg.vpcId).filter(Boolean))].sort();
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

  openDetail(sg: SecurityGroup): void {
    this.selectedGroup = sg;
    this.activeRulesTab = 'inbound';
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedGroup = null, 300);
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedAccount = 'all';
    this.selectedVpc = 'all';
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
      this.cloudServicesService.syncSecurityGroups(a.id).pipe(catchError(() => of(null)))
    );

    forkJoin(syncRequests).subscribe({
      next: (results) => {
        this.syncing = false;
        this.lastSyncResults = results.filter((r): r is SecurityGroupSyncResponse => r !== null);
        const totalAdded = this.getTotalSyncAdded();
        const totalGroups = this.getTotalSyncGroups();
        this.syncMessage = `Sync completed — ${awsAccounts.length} account${awsAccounts.length !== 1 ? 's' : ''} · ${totalGroups} group${totalGroups !== 1 ? 's' : ''} · +${totalAdded} added`;
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

  getTotalSyncGroups(): number {
    return this.lastSyncResults.reduce((sum, r) => sum + (r.securityGroups?.length || 0), 0);
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS';
      case 'AZURE': return 'Azure NSG';
      case 'GCP': return 'GCP Firewall';
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

  getProtocolLabel(protocol: string): string {
    if (!protocol || protocol === '-1') return 'All Traffic';
    return protocol.toUpperCase();
  }

  getPortRange(rule: SecurityGroupRule): string {
    if (!rule.protocol || rule.protocol === '-1') return 'All';
    if (rule.fromPort === rule.toPort) return rule.fromPort?.toString() || '—';
    return `${rule.fromPort}-${rule.toPort}`;
  }

  getRuleSource(rule: SecurityGroupRule): string {
    const sources: string[] = [];
    if (rule.ipv4Ranges?.length) sources.push(...rule.ipv4Ranges);
    if (rule.ipv6Ranges?.length) sources.push(...rule.ipv6Ranges);
    if (rule.referencedGroupIds?.length) sources.push(...rule.referencedGroupIds);
    return sources.length > 0 ? sources.join(', ') : '—';
  }

  getRuleSourceShort(rule: SecurityGroupRule): string {
    const src = this.getRuleSource(rule);
    if (src.length > 40) return src.substring(0, 37) + '…';
    return src;
  }

  isOpenToWorld(rule: SecurityGroupRule): boolean {
    return rule.ipv4Ranges?.includes('0.0.0.0/0') || rule.ipv6Ranges?.includes('::/0') || false;
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
      this.selectedAccount !== 'all' ||
      this.selectedVpc !== 'all';
  }

  get totalInboundRules(): number {
    return this.securityGroups.reduce((sum, sg) => sum + (sg.inboundRules?.length || 0), 0);
  }

  get totalOutboundRules(): number {
    return this.securityGroups.reduce((sum, sg) => sum + (sg.outboundRules?.length || 0), 0);
  }

  get groupsWithOpenInbound(): number {
    return this.securityGroups.filter(sg =>
      sg.inboundRules?.some(r => this.isOpenToWorld(r))
    ).length;
  }
}
