import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { KeyPair, UpdateKeyPairTagsRequest } from '../../../models/cloud-services.model';

@Component({
  selector: 'app-key-pair-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './key-pair-overview.component.html',
  styleUrls: ['./key-pair-overview.component.css']
})
export class KeyPairOverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  keyPair: KeyPair | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];

  collapsedSections: Record<string, boolean> = {};

  copiedField: string | null = null;
  private copiedTimeout: any;

  // Update Tags
  showUpdatePanel = false;
  updating = false;
  updateError: string | null = null;
  updateSuccess: string | null = null;
  tagEntries: { key: string; value: string }[] = [];

  // Delete
  showDeleteConfirm = false;
  deleting = false;
  deleteError: string | null = null;

  // Toast
  toastMessage: string | null = null;
  toastType: 'success' | 'error' | 'info' | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.error = 'Invalid key pair ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(keyPairId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findKeyPair(accounts, keyPairId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findKeyPair(accounts: CloudAccount[], keyPairId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }
    const requests = accounts.map(a =>
      this.cloudServicesService.getKeyPairs(a.id).pipe(catchError(() => of({ keyPairs: [] })))
    );
    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const all: KeyPair[] = results.flatMap((r: any) => r.keyPairs || []);
        this.keyPair = all.find(k => k.id === keyPairId) || null;
        if (this.keyPair) {
          this.account = accounts.find(a => a.id === this.keyPair!.cloudAccountId) || null;
        } else {
          this.error = 'Key pair not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load key pair data';
        this.loading = false;
      }
    });
  }

  refreshData(): void {
    if (this.keyPair) this.loadData(this.keyPair.id);
  }

  toggleSection(section: string): void {
    this.collapsedSections[section] = !this.collapsedSections[section];
  }

  isSectionCollapsed(section: string): boolean {
    return !!this.collapsedSections[section];
  }

  copyToClipboard(text: string, field: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedField = field;
      if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
      this.copiedTimeout = setTimeout(() => this.copiedField = null, 2000);
    });
  }

  // ── Tags Management ──

  getTagEntries(): { key: string; value: string }[] {
    if (!this.keyPair?.tags) return [];
    return Object.entries(this.keyPair.tags).map(([key, value]) => ({ key, value }));
  }

  getTagCount(): number {
    return this.keyPair?.tags ? Object.keys(this.keyPair.tags).length : 0;
  }

  openUpdatePanel(): void {
    this.showUpdatePanel = true;
    this.updateError = null;
    this.updateSuccess = null;
    this.tagEntries = this.getTagEntries();
    if (this.tagEntries.length === 0) this.addTag();
    setTimeout(() => {
      document.getElementById('updatePanel')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
  }

  closeUpdatePanel(): void {
    this.showUpdatePanel = false;
    this.updateError = null;
    this.updateSuccess = null;
  }

  addTag(): void {
    this.tagEntries.push({ key: '', value: '' });
  }

  removeTag(index: number): void {
    this.tagEntries.splice(index, 1);
  }

  updateTags(): void {
    if (!this.keyPair || this.updating) return;
    const tags: { [key: string]: string } = {};
    for (const entry of this.tagEntries) {
      if (entry.key.trim()) tags[entry.key.trim()] = entry.value.trim();
    }
    this.updating = true;
    this.updateError = null;
    this.updateSuccess = null;
    const body: UpdateKeyPairTagsRequest = { tags };
    this.cloudServicesService.updateKeyPairTags(this.keyPair.id, this.keyPair.cloudAccountId, body).subscribe({
      next: (updated) => {
        this.keyPair = updated;
        this.updating = false;
        this.updateSuccess = 'Tags updated successfully.';
        setTimeout(() => this.updateSuccess = null, 4000);
      },
      error: (err) => {
        this.updating = false;
        this.updateError = err?.error?.message || 'Failed to update tags.';
      }
    });
  }

  // ── Delete ──

  openDeleteConfirm(): void {
    this.showDeleteConfirm = true;
    this.deleteError = null;
  }

  closeDeleteConfirm(): void {
    this.showDeleteConfirm = false;
    this.deleteError = null;
  }

  deleteKeyPair(): void {
    if (!this.keyPair || this.deleting) return;
    this.deleting = true;
    this.deleteError = null;
    this.cloudServicesService.deleteKeyPair(this.keyPair.id, this.keyPair.cloudAccountId).subscribe({
      next: () => {
        this.deleting = false;
        this.router.navigate(['/dashboard/key-pairs']);
      },
      error: (err) => {
        this.deleting = false;
        this.deleteError = err?.error?.message || 'Failed to delete key pair.';
      }
    });
  }

  // ── Download ──

  viewPrivateKey(): void {
    if (!this.keyPair) return;
    this.router.navigate(['/dashboard/key-pairs', this.keyPair.id, 'private-key']);
  }

  downloadPublicKey(): void {
    if (!this.keyPair?.publicKeyMaterial) return;
    const blob = new Blob([this.keyPair.publicKeyMaterial], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${this.keyPair.keyName}.pub`;
    a.click();
    URL.revokeObjectURL(url);
  }

  // ── Import (Under Development) ──

  importKeyPair(): void {
    this.showToast('This feature is Under Development', 'info');
  }

  showToast(msg: string, type: 'success' | 'error' | 'info'): void {
    this.toastMessage = msg;
    this.toastType = type;
    setTimeout(() => { this.toastMessage = null; this.toastType = null; }, 4000);
  }

  // ── Utility ──

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(dateStr));
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(new Date(dateStr));
  }

  formatRelativeTime(dateStr: string): string {
    if (!dateStr) return '—';
    const diffMs = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diffMs / 60000);
    const hours = Math.floor(diffMs / 3600000);
    const days = Math.floor(diffMs / 86400000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days < 30) return `${days}d ago`;
    return this.formatDate(dateStr);
  }

  getKeyTypeClass(type: string): string {
    const t = type?.toLowerCase();
    if (t === 'rsa') return 'bg-blue-50 text-blue-700 border-blue-200';
    if (t === 'ed25519') return 'bg-purple-50 text-purple-700 border-purple-200';
    return 'bg-gray-50 text-gray-700 border-gray-200';
  }
}
