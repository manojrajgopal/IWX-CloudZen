import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { LogGroup } from '../../../models/cloud-services.model';
import { LogGroupFilterByProviderPipe, LogGroupFilterByClassPipe } from './cloudwatch-logs.pipes';

type ViewMode = 'grid' | 'list';
type SortField = 'logGroupName' | 'storedBytes' | 'retentionInDays' | 'logGroupClass' | 'creationTimeUtc' | 'createdAt';
type SortDir = 'asc' | 'desc';

interface LogMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-cloudwatch-logs',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LogGroupFilterByProviderPipe, LogGroupFilterByClassPipe],
  templateUrl: './cloudwatch-logs.component.html',
  styleUrls: ['./cloudwatch-logs.component.css']
})
export class CloudWatchLogsComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  logGroups: LogGroup[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedAccount = 'all';
  selectedClass = 'all';
  selectedRetention = 'all';
  selectedProtection = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'logGroupName';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedLogGroup: LogGroup | null = null;
  showDetailPanel = false;

  // Metrics
  metrics: LogMetric[] = [
    { label: 'Total Log Groups', value: '—', icon: 'M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z', color: 'text-black' },
    { label: 'Total Storage', value: '—', icon: 'M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375', color: 'text-blue-600' },
    { label: 'With Retention', value: '—', icon: 'M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z', color: 'text-green-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS CloudWatch' },
    { value: 'Azure', label: 'Azure Monitor' },
    { value: 'GCP', label: 'GCP Cloud Logging' }
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
        this.loadLogGroups(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadLogGroups(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getLogGroups(a.id).pipe(catchError(() => of({ logGroups: [] })))
    );

    forkJoin(requests).subscribe(results => {
      this.logGroups = results.flatMap((r: any) => r.logGroups || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.logGroups.length.toString();
    this.metrics[1].value = this.formatBytes(this.logGroups.reduce((sum, lg) => sum + (lg.storedBytes || 0), 0));
    this.metrics[2].value = this.logGroups.filter(lg => lg.retentionInDays !== null && lg.retentionInDays > 0).length.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredLogGroups(): LogGroup[] {
    let result = [...this.logGroups];

    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(lg =>
        lg.logGroupName?.toLowerCase().includes(q) ||
        lg.arn?.toLowerCase().includes(q) ||
        lg.logGroupClass?.toLowerCase().includes(q) ||
        lg.provider?.toLowerCase().includes(q)
      );
    }

    if (this.selectedProvider !== 'all') {
      result = result.filter(lg => lg.provider === this.selectedProvider);
    }

    if (this.selectedAccount !== 'all') {
      result = result.filter(lg => lg.cloudAccountId === +this.selectedAccount);
    }

    if (this.selectedClass !== 'all') {
      result = result.filter(lg => lg.logGroupClass === this.selectedClass);
    }

    if (this.selectedRetention !== 'all') {
      if (this.selectedRetention === 'never') {
        result = result.filter(lg => lg.retentionInDays === null || lg.retentionInDays === 0);
      } else {
        result = result.filter(lg => lg.retentionInDays !== null && lg.retentionInDays > 0);
      }
    }

    if (this.selectedProtection !== 'all') {
      if (this.selectedProtection === 'protected') {
        result = result.filter(lg => lg.dataProtectionStatus && lg.dataProtectionStatus !== 'DISABLED');
      } else {
        result = result.filter(lg => !lg.dataProtectionStatus || lg.dataProtectionStatus === 'DISABLED');
      }
    }

    result.sort((a, b) => {
      let cmp = 0;
      switch (this.sortField) {
        case 'logGroupName': cmp = (a.logGroupName || '').localeCompare(b.logGroupName || ''); break;
        case 'storedBytes': cmp = (a.storedBytes || 0) - (b.storedBytes || 0); break;
        case 'retentionInDays': cmp = (a.retentionInDays || 0) - (b.retentionInDays || 0); break;
        case 'logGroupClass': cmp = (a.logGroupClass || '').localeCompare(b.logGroupClass || ''); break;
        case 'creationTimeUtc': cmp = (a.creationTimeUtc || '').localeCompare(b.creationTimeUtc || ''); break;
        case 'createdAt': cmp = (a.createdAt || '').localeCompare(b.createdAt || ''); break;
        default: cmp = (a.logGroupName || '').localeCompare(b.logGroupName || '');
      }
      return this.sortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  get uniqueClasses(): string[] {
    return [...new Set(this.logGroups.map(lg => lg.logGroupClass).filter(Boolean))].sort();
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

  openDetail(lg: LogGroup): void {
    this.selectedLogGroup = lg;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    document.body.style.overflow = '';
    setTimeout(() => this.selectedLogGroup = null, 300);
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedAccount = 'all';
    this.selectedClass = 'all';
    this.selectedRetention = 'all';
    this.selectedProtection = 'all';
  }

  refreshData(): void {
    this.loadData();
  }

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS CloudWatch';
      case 'AZURE': return 'Azure Monitor';
      case 'GCP': return 'GCP Cloud Logging';
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

  formatBytes(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    const val = bytes / Math.pow(1024, i);
    return val.toFixed(i > 0 ? 1 : 0) + ' ' + units[i];
  }

  getRetentionLabel(days: number | null): string {
    if (days === null || days === 0) return 'Never expire';
    if (days === 1) return '1 day';
    if (days < 30) return `${days} days`;
    if (days < 365) return `${Math.round(days / 30)} months`;
    return `${Math.round(days / 365)} years`;
  }

  getRetentionClass(days: number | null): string {
    if (days === null || days === 0) return 'bg-yellow-50 text-yellow-700';
    if (days <= 30) return 'bg-red-50 text-red-700';
    if (days <= 90) return 'bg-orange-50 text-orange-700';
    return 'bg-green-50 text-green-700';
  }

  getClassLabel(logGroupClass: string): string {
    switch (logGroupClass?.toUpperCase()) {
      case 'STANDARD': return 'Standard';
      case 'INFREQUENT_ACCESS': return 'Infrequent Access';
      default: return logGroupClass || '—';
    }
  }

  getClassBadgeClass(logGroupClass: string): string {
    switch (logGroupClass?.toUpperCase()) {
      case 'STANDARD': return 'bg-blue-50 text-blue-700';
      case 'INFREQUENT_ACCESS': return 'bg-purple-50 text-purple-700';
      default: return 'bg-gray-50 text-gray-500';
    }
  }

  getProtectionLabel(status: string | null): string {
    if (!status || status === 'DISABLED') return 'Disabled';
    return status.charAt(0) + status.slice(1).toLowerCase();
  }

  getProtectionClass(status: string | null): string {
    if (!status || status === 'DISABLED') return 'bg-gray-50 text-gray-500';
    return 'bg-green-50 text-green-700';
  }

  getLogGroupShortName(name: string): string {
    if (!name) return '—';
    const parts = name.split('/');
    return parts[parts.length - 1] || name;
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
      this.selectedClass !== 'all' ||
      this.selectedRetention !== 'all' ||
      this.selectedProtection !== 'all';
  }

  get totalStoredBytes(): number {
    return this.logGroups.reduce((sum, lg) => sum + (lg.storedBytes || 0), 0);
  }

  get withRetentionCount(): number {
    return this.logGroups.filter(lg => lg.retentionInDays !== null && lg.retentionInDays > 0).length;
  }

  get neverExpireCount(): number {
    return this.logGroups.filter(lg => lg.retentionInDays === null || lg.retentionInDays === 0).length;
  }

  get encryptedCount(): number {
    return this.logGroups.filter(lg => lg.kmsKeyId).length;
  }
}
