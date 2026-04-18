import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../services/cloud-account.service';
import { CloudServicesService } from '../../services/cloud-services.service';
import { CloudAccount } from '../../models/cloud-account.model';
import { KeyPair, KeyPairSyncResponse } from '../../models/cloud-services.model';

type ViewMode = 'grid' | 'list';
type SortField = 'keyName' | 'keyType' | 'createdAt' | 'awsCreatedAt';
type SortDir = 'asc' | 'desc';

interface KpMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-key-pairs',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './key-pairs.component.html',
  styleUrls: ['./key-pairs.component.css']
})
export class KeyPairsComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  keyPairs: KeyPair[] = [];
  loading = true;

  // Filters
  searchQuery = '';
  selectedProvider = 'all';
  selectedAccount = 'all';
  selectedKeyType = 'all';
  selectedPrivateKey = 'all';

  // View
  viewMode: ViewMode = 'list';
  sortField: SortField = 'keyName';
  sortDir: SortDir = 'asc';

  // Detail panel
  selectedKeyPair: KeyPair | null = null;
  showDetailPanel = false;

  // Sync state
  syncing = false;
  lastSyncResults: KeyPairSyncResponse[] = [];
  showSyncReport = false;
  syncMessage: string | null = null;
  syncMessageType: 'success' | 'error' | null = null;

  // Delete state
  deletingKeyPairId: number | null = null;
  showDeleteConfirm = false;
  keyPairToDelete: KeyPair | null = null;
  deleteMessage: string | null = null;
  deleteMessageType: 'success' | 'error' | null = null;

  // Metrics
  metrics: KpMetric[] = [
    { label: 'Total Key Pairs', value: '—', icon: 'M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z', color: 'text-black' },
    { label: 'RSA Keys', value: '—', icon: 'M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z', color: 'text-blue-600' },
    { label: 'With Private Key', value: '—', icon: 'M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z', color: 'text-green-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-purple-600' }
  ];

  providers = [
    { value: 'all', label: 'All Providers' },
    { value: 'AWS', label: 'AWS' }
  ];

  keyTypeOptions = [
    { value: 'all', label: 'All Types' },
    { value: 'rsa', label: 'RSA' },
    { value: 'ed25519', label: 'ED25519' }
  ];

  privateKeyOptions = [
    { value: 'all', label: 'All Keys' },
    { value: 'yes', label: 'Has Private Key' },
    { value: 'no', label: 'No Private Key' }
  ];

  constructor(
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  // ── Data Loading ──

  private loadData(): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.loadKeyPairs(accounts);
      },
      error: () => {
        this.loading = false;
        this.updateMetrics();
      }
    });
  }

  private loadKeyPairs(accounts: CloudAccount[]): void {
    if (accounts.length === 0) {
      this.loading = false;
      this.updateMetrics();
      return;
    }
    const requests = accounts.map(a =>
      this.cloudServicesService.getKeyPairs(a.id).pipe(catchError(() => of({ keyPairs: [] })))
    );
    forkJoin(requests).subscribe(results => {
      this.keyPairs = results.flatMap((r: any) => r.keyPairs || []);
      this.loading = false;
      this.updateMetrics();
    });
  }

  private updateMetrics(): void {
    this.metrics[0].value = this.keyPairs.length.toString();
    this.metrics[1].value = this.keyPairs.filter(k => k.keyType?.toLowerCase() === 'rsa').length.toString();
    this.metrics[2].value = this.keyPairs.filter(k => k.hasPrivateKey).length.toString();
    this.metrics[3].value = this.accounts.length.toString();
  }

  // ── Filtering & Sorting ──

  get filteredKeyPairs(): KeyPair[] {
    let result = [...this.keyPairs];
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(k =>
        k.keyName.toLowerCase().includes(q) ||
        k.keyPairId.toLowerCase().includes(q) ||
        k.keyFingerprint.toLowerCase().includes(q)
      );
    }
    if (this.selectedProvider !== 'all') result = result.filter(k => k.provider === this.selectedProvider);
    if (this.selectedAccount !== 'all') result = result.filter(k => k.cloudAccountId === +this.selectedAccount);
    if (this.selectedKeyType !== 'all') result = result.filter(k => k.keyType?.toLowerCase() === this.selectedKeyType);
    if (this.selectedPrivateKey !== 'all') {
      result = result.filter(k => this.selectedPrivateKey === 'yes' ? k.hasPrivateKey : !k.hasPrivateKey);
    }
    result.sort((a, b) => {
      let valA = '', valB = '';
      switch (this.sortField) {
        case 'keyName': valA = a.keyName; valB = b.keyName; break;
        case 'keyType': valA = a.keyType || ''; valB = b.keyType || ''; break;
        case 'createdAt': valA = a.createdAt; valB = b.createdAt; break;
        case 'awsCreatedAt': valA = a.awsCreatedAt; valB = b.awsCreatedAt; break;
      }
      const cmp = valA.localeCompare(valB);
      return this.sortDir === 'asc' ? cmp : -cmp;
    });
    return result;
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery !== '' || this.selectedProvider !== 'all' || this.selectedAccount !== 'all' || this.selectedKeyType !== 'all' || this.selectedPrivateKey !== 'all';
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedProvider = 'all';
    this.selectedAccount = 'all';
    this.selectedKeyType = 'all';
    this.selectedPrivateKey = 'all';
  }

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

  refreshData(): void {
    this.loadData();
  }

  // ── Detail Panel ──

  openDetail(kp: KeyPair): void {
    this.selectedKeyPair = kp;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    setTimeout(() => {
      this.selectedKeyPair = null;
      document.body.style.overflow = '';
    }, 300);
  }

  goToOverview(kp: KeyPair): void {
    this.closeDetail();
    this.router.navigate(['/dashboard/key-pairs', kp.id]);
  }

  // ── Sync ──

  syncData(): void {
    this.syncing = true;
    this.lastSyncResults = [];
    const requests = this.accounts.map(a =>
      this.cloudServicesService.syncKeyPairs(a.id).pipe(catchError(() => of(null)))
    );
    forkJoin(requests).subscribe(results => {
      this.lastSyncResults = results.filter((r): r is KeyPairSyncResponse => r !== null);
      const totalAdded = this.lastSyncResults.reduce((s, r) => s + r.added, 0);
      const totalRemoved = this.lastSyncResults.reduce((s, r) => s + r.removed, 0);
      const totalUpdated = this.lastSyncResults.reduce((s, r) => s + r.updated, 0);
      this.syncing = false;
      this.showSyncReport = true;
      this.showSyncMessage(`Sync complete: ${totalAdded} added, ${totalUpdated} updated, ${totalRemoved} removed`, 'success');
      this.loadData();
    });
  }

  closeSyncReport(): void { this.showSyncReport = false; }
  getTotalSyncAdded(): number { return this.lastSyncResults.reduce((s, r) => s + r.added, 0); }
  getTotalSyncUpdated(): number { return this.lastSyncResults.reduce((s, r) => s + r.updated, 0); }
  getTotalSyncRemoved(): number { return this.lastSyncResults.reduce((s, r) => s + r.removed, 0); }
  getTotalSyncKeys(): number { return this.lastSyncResults.reduce((s, r) => s + r.keyPairs.length, 0); }

  // ── Delete ──

  confirmDelete(kp: KeyPair): void {
    this.keyPairToDelete = kp;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.keyPairToDelete = null;
  }

  deleteKeyPair(): void {
    if (!this.keyPairToDelete) return;
    const kp = this.keyPairToDelete;
    this.deletingKeyPairId = kp.id;
    this.cloudServicesService.deleteKeyPair(kp.id, kp.cloudAccountId).subscribe({
      next: () => {
        this.deletingKeyPairId = null;
        this.showDeleteConfirm = false;
        this.keyPairToDelete = null;
        if (this.selectedKeyPair?.id === kp.id) this.closeDetail();
        this.showDeleteMsg('Key pair deleted successfully.', 'success');
        this.loadData();
      },
      error: (err) => {
        this.deletingKeyPairId = null;
        this.showDeleteMsg(err?.error?.message || 'Failed to delete key pair.', 'error');
      }
    });
  }

  // ── Download ──

  downloadPrivateKey(kp: KeyPair): void {
    this.router.navigate(['/dashboard/key-pairs', kp.id, 'private-key']);
  }

  downloadPublicKey(kp: KeyPair): void {
    if (!kp.publicKeyMaterial) return;
    const blob = new Blob([kp.publicKeyMaterial], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${kp.keyName}.pub`;
    a.click();
    URL.revokeObjectURL(url);
  }

  // ── Toast Messages ──

  showSyncMessage(msg: string, type: 'success' | 'error'): void {
    this.syncMessage = msg;
    this.syncMessageType = type;
    setTimeout(() => { this.syncMessage = null; this.syncMessageType = null; }, 5000);
  }

  showDeleteMsg(msg: string, type: 'success' | 'error'): void {
    this.deleteMessage = msg;
    this.deleteMessageType = type;
    setTimeout(() => { this.deleteMessage = null; this.deleteMessageType = null; }, 5000);
  }

  // ── Utility ──

  getAccountName(accountId: number): string {
    return this.accounts.find(a => a.id === accountId)?.accountName || `Account #${accountId}`;
  }

  getTagCount(kp: KeyPair): number {
    return kp.tags ? Object.keys(kp.tags).length : 0;
  }

  getTagEntries(tags: { [key: string]: string }): { key: string; value: string }[] {
    if (!tags) return [];
    return Object.entries(tags).map(([key, value]) => ({ key, value }));
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(dateStr));
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(dateStr));
  }

  truncateFingerprint(fp: string): string {
    if (!fp || fp.length <= 30) return fp;
    return fp.substring(0, 14) + '...' + fp.substring(fp.length - 14);
  }

  getKeyTypeClass(type: string): string {
    const t = type?.toLowerCase();
    if (t === 'rsa') return 'bg-blue-50 text-blue-700 border-blue-200';
    if (t === 'ed25519') return 'bg-purple-50 text-purple-700 border-purple-200';
    return 'bg-gray-50 text-gray-700 border-gray-200';
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text);
    this.showSyncMessage('Copied to clipboard', 'success');
  }
}
