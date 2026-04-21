import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { InternetGateway, Vpc } from '../../../../models/cloud-services.model';

@Component({
  selector: 'app-internet-gateway-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './internet-gateway-overview.component.html',
  styleUrls: ['./internet-gateway-overview.component.css']
})
export class InternetGatewayOverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  igw: InternetGateway | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];
  vpcs: Vpc[] = [];
  attachedVpc: Vpc | null = null;

  // Section collapse
  collapsedSections: Record<string, boolean> = {};

  // Clipboard
  copiedField: string | null = null;
  private copiedTimeout: any;

  // Rename state
  showRenamePanel = false;
  renaming = false;
  renameError: string | null = null;
  renameSuccess: string | null = null;
  newName = '';

  // Attach/Detach state
  showAttachPanel = false;
  attaching = false;
  attachError: string | null = null;
  attachSuccess: string | null = null;
  selectedAttachVpcId: string | null = null;

  // Delete state
  showDeleteConfirm = false;
  deleting = false;
  deleteError: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.error = 'Invalid Internet Gateway ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(igwId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findIgw(accounts, igwId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findIgw(accounts: CloudAccount[], igwId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const igwRequests = accounts.map(a =>
      this.cloudServicesService.getInternetGateways(a.id).pipe(catchError(() => of({ internetGateways: [] })))
    );
    const vpcRequests = accounts.map(a =>
      this.cloudServicesService.getVpcs(a.id).pipe(catchError(() => of({ vpcs: [] })))
    );

    forkJoin([...igwRequests, ...vpcRequests]).subscribe({
      next: (results: any[]) => {
        const igwResults = results.slice(0, accounts.length);
        const vpcResults = results.slice(accounts.length);
        const allIgws: InternetGateway[] = igwResults.flatMap((r: any) => r.internetGateways || []);
        this.vpcs = vpcResults.flatMap((r: any) => r.vpcs || []);
        this.igw = allIgws.find(i => i.id === igwId) || null;

        if (this.igw) {
          this.account = accounts.find(a => a.id === this.igw!.cloudAccountId) || null;
          if (this.igw.attachedVpcId) {
            this.attachedVpc = this.vpcs.find(v => v.vpcId === this.igw!.attachedVpcId) || null;
          }
        } else {
          this.error = 'Internet Gateway not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load Internet Gateway data';
        this.loading = false;
      }
    });
  }

  refreshData(): void {
    if (this.igw) {
      this.loadData(this.igw.id);
    }
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

  // ── Display Helpers ──

  getDisplayName(igw: InternetGateway): string {
    return igw.name || igw.internetGatewayId || '—';
  }

  getVpcDisplayName(vpcId: string | null): string {
    if (!vpcId) return '—';
    const vpc = this.vpcs.find(v => v.vpcId === vpcId);
    return vpc?.name || vpcId;
  }

  getStateBgClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'attached'].includes(s)) return 'bg-green-50 border-green-200';
    if (['pending', 'attaching', 'detaching'].includes(s)) return 'bg-yellow-50 border-yellow-200';
    if (['detached', 'deleted', 'error', 'failed'].includes(s)) return 'bg-red-50 border-red-200';
    return 'bg-gray-50 border-gray-200';
  }

  getStateTextClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'attached'].includes(s)) return 'text-green-700';
    if (['pending', 'attaching', 'detaching'].includes(s)) return 'text-yellow-700';
    if (['detached', 'deleted', 'error', 'failed'].includes(s)) return 'text-red-700';
    return 'text-gray-600';
  }

  getStateDotClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'attached'].includes(s)) return 'bg-green-500';
    if (['pending', 'attaching', 'detaching'].includes(s)) return 'bg-yellow-500';
    if (['detached', 'deleted', 'error', 'failed'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'Amazon Web Services';
      default: return provider || 'Unknown';
    }
  }

  getProviderShort(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS';
      default: return provider || '—';
    }
  }

  getProviderBgClass(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'bg-orange-50 border-orange-200 text-orange-700';
      default: return 'bg-gray-50 border-gray-200 text-gray-600';
    }
  }

  get isAttached(): boolean {
    return !!this.igw?.attachedVpcId && this.igw?.state?.toLowerCase() !== 'detached';
  }

  get availableVpcsForAttach(): Vpc[] {
    return this.vpcs.filter(v => v.cloudAccountId === this.igw?.cloudAccountId);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
  }

  formatRelativeTime(dateStr: string): string {
    if (!dateStr) return '—';
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins !== 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`;
    if (diffDays < 30) return `${diffDays} day${diffDays !== 1 ? 's' : ''} ago`;
    return this.formatDate(dateStr);
  }

  getUptime(dateStr: string): string {
    if (!dateStr) return '—';
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const days = Math.floor(diffMs / 86400000);
    const hours = Math.floor((diffMs % 86400000) / 3600000);
    const minutes = Math.floor((diffMs % 3600000) / 60000);
    if (days > 0) return `${days}d ${hours}h ${minutes}m`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
  }

  // ── Rename ──

  openRenamePanel(): void {
    this.showRenamePanel = true;
    this.renameError = null;
    this.renameSuccess = null;
    this.newName = this.igw?.name || '';
  }

  closeRenamePanel(): void {
    this.showRenamePanel = false;
    this.renameError = null;
    this.renameSuccess = null;
  }

  renameIgw(): void {
    if (!this.igw || this.renaming || !this.newName.trim()) return;

    this.renaming = true;
    this.renameError = null;
    this.renameSuccess = null;

    this.cloudServicesService.updateInternetGateway(
      this.igw.id,
      this.igw.cloudAccountId,
      { name: this.newName.trim() }
    ).subscribe({
      next: (updated) => {
        this.igw = updated;
        this.renaming = false;
        this.renameSuccess = 'Internet Gateway renamed successfully.';
        setTimeout(() => this.renameSuccess = null, 4000);
      },
      error: (err) => {
        this.renaming = false;
        this.renameError = err?.error?.message || err?.error?.title || 'Failed to rename. Please try again.';
      }
    });
  }

  // ── Attach ──

  openAttachPanel(): void {
    this.showAttachPanel = true;
    this.attachError = null;
    this.attachSuccess = null;
    this.selectedAttachVpcId = null;
  }

  closeAttachPanel(): void {
    this.showAttachPanel = false;
    this.attachError = null;
    this.attachSuccess = null;
  }

  selectAttachVpc(vpcId: string): void {
    this.selectedAttachVpcId = this.selectedAttachVpcId === vpcId ? null : vpcId;
  }

  attachToVpc(): void {
    if (!this.igw || this.attaching || !this.selectedAttachVpcId) return;

    this.attaching = true;
    this.attachError = null;

    this.cloudServicesService.attachInternetGateway(
      this.igw.id,
      this.igw.cloudAccountId,
      { vpcId: this.selectedAttachVpcId }
    ).subscribe({
      next: (updated) => {
        this.igw = updated;
        this.attachedVpc = this.vpcs.find(v => v.vpcId === updated.attachedVpcId) || null;
        this.attaching = false;
        this.attachSuccess = 'Internet Gateway attached to VPC successfully.';
        this.showAttachPanel = false;
        setTimeout(() => this.attachSuccess = null, 4000);
      },
      error: (err) => {
        this.attaching = false;
        this.attachError = err?.error?.message || err?.error?.title || 'Failed to attach. Please try again.';
      }
    });
  }

  // ── Detach ──

  detachFromVpc(): void {
    if (!this.igw || this.attaching || !this.igw.attachedVpcId) return;

    this.attaching = true;
    this.attachError = null;

    this.cloudServicesService.detachInternetGateway(
      this.igw.id,
      this.igw.cloudAccountId,
      { vpcId: this.igw.attachedVpcId }
    ).subscribe({
      next: (updated) => {
        this.igw = updated;
        this.attachedVpc = null;
        this.attaching = false;
        this.attachSuccess = 'Internet Gateway detached from VPC successfully.';
        setTimeout(() => this.attachSuccess = null, 4000);
      },
      error: (err) => {
        this.attaching = false;
        this.attachError = err?.error?.message || err?.error?.title || 'Failed to detach. Please try again.';
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

  deleteIgw(): void {
    if (!this.igw || this.deleting) return;

    this.deleting = true;
    this.deleteError = null;

    this.cloudServicesService.deleteInternetGateway(
      this.igw.id,
      this.igw.cloudAccountId
    ).subscribe({
      next: () => {
        this.deleting = false;
        this.router.navigate(['/dashboard/internet-gateways']);
      },
      error: (err) => {
        this.deleting = false;
        this.deleteError = err?.error?.message || err?.error?.title || 'Failed to delete. Please try again.';
      }
    });
  }
}
