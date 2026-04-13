import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Vpc, UpdateVpcRequest } from '../../../../models/cloud-services.model';

@Component({
  selector: 'app-vpc-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './vpc-overview.component.html',
  styleUrls: ['./vpc-overview.component.css']
})
export class VpcOverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  vpc: Vpc | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];

  // Section collapse states
  collapsedSections: Record<string, boolean> = {};

  // Copied tooltip
  copiedField: string | null = null;
  private copiedTimeout: any;

  // Update state
  showUpdatePanel = false;
  updating = false;
  updateError: string | null = null;
  updateSuccess: string | null = null;
  updateFields: {
    enableDnsSupport: boolean | null;
    enableDnsHostnames: boolean | null;
    amazonProvidedIpv6CidrBlock: boolean | null;
  } = {
    enableDnsSupport: null,
    enableDnsHostnames: null,
    amazonProvidedIpv6CidrBlock: null
  };

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
      this.error = 'Invalid VPC ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(vpcId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findVpc(accounts, vpcId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findVpc(accounts: CloudAccount[], vpcId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getVpcs(a.id).pipe(catchError(() => of({ vpcs: [] })))
    );

    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const allVpcs: Vpc[] = results.flatMap((r: any) => r.vpcs || []);
        this.vpc = allVpcs.find(v => v.id === vpcId) || null;

        if (this.vpc) {
          this.account = accounts.find(a => a.id === this.vpc!.cloudAccountId) || null;
        } else {
          this.error = 'VPC not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load VPC data';
        this.loading = false;
      }
    });
  }

  refreshData(): void {
    if (this.vpc) {
      this.loadData(this.vpc.id);
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

  getDisplayName(vpc: Vpc): string {
    return vpc.name || vpc.vpcId || '—';
  }

  getStateBgClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'active'].includes(s)) return 'bg-green-50 border-green-200';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-50 border-yellow-200';
    if (['deleted', 'terminated', 'error', 'failed'].includes(s)) return 'bg-red-50 border-red-200';
    return 'bg-gray-50 border-gray-200';
  }

  getStateTextClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'active'].includes(s)) return 'text-green-700';
    if (['pending', 'creating', 'updating'].includes(s)) return 'text-yellow-700';
    if (['deleted', 'terminated', 'error', 'failed'].includes(s)) return 'text-red-700';
    return 'text-gray-600';
  }

  getStateDotClass(state: string): string {
    const s = state?.toLowerCase();
    if (['available', 'active'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-500';
    if (['deleted', 'terminated', 'error', 'failed'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'Amazon Web Services';
      case 'AZURE': return 'Microsoft Azure';
      case 'GCP': return 'Google Cloud Platform';
      default: return provider || 'Unknown';
    }
  }

  getProviderShort(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS';
      case 'AZURE': return 'Azure';
      case 'GCP': return 'GCP';
      default: return provider || '—';
    }
  }

  getProviderBgClass(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'bg-orange-50 border-orange-200 text-orange-700';
      case 'AZURE': return 'bg-blue-50 border-blue-200 text-blue-700';
      case 'GCP': return 'bg-red-50 border-red-200 text-red-700';
      default: return 'bg-gray-50 border-gray-200 text-gray-600';
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

  getCidrIpCount(cidr: string): string {
    if (!cidr) return '—';
    const parts = cidr.split('/');
    if (parts.length !== 2) return '—';
    const prefix = parseInt(parts[1], 10);
    if (isNaN(prefix) || prefix < 0 || prefix > 32) return '—';
    const count = Math.pow(2, 32 - prefix);
    return count.toLocaleString();
  }

  getCidrNetwork(cidr: string): string {
    if (!cidr) return '—';
    return cidr.split('/')[0] || '—';
  }

  getCidrPrefix(cidr: string): string {
    if (!cidr) return '—';
    return '/' + (cidr.split('/')[1] || '—');
  }

  getCidrClass(cidr: string): string {
    if (!cidr) return '—';
    const prefix = parseInt(cidr.split('/')[1], 10);
    if (isNaN(prefix)) return '—';
    if (prefix <= 8) return 'Class A';
    if (prefix <= 16) return 'Class B';
    if (prefix <= 24) return 'Class C';
    return 'Subnet';
  }

  // ── Update ──

  openUpdatePanel(): void {
    this.showUpdatePanel = true;
    this.updateError = null;
    this.updateSuccess = null;
    this.updateFields = {
      enableDnsSupport: this.vpc?.enableDnsSupport ?? null,
      enableDnsHostnames: this.vpc?.enableDnsHostnames ?? null,
      amazonProvidedIpv6CidrBlock: null
    };
    setTimeout(() => {
      document.getElementById('updatePanel')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
  }

  closeUpdatePanel(): void {
    this.showUpdatePanel = false;
    this.updateError = null;
    this.updateSuccess = null;
  }

  updateVpc(): void {
    if (!this.vpc || this.updating) return;

    const body: Record<string, any> = {};
    if (this.updateFields.enableDnsSupport !== null && this.updateFields.enableDnsSupport !== this.vpc.enableDnsSupport) {
      body['enableDnsSupport'] = this.updateFields.enableDnsSupport;
    }
    if (this.updateFields.enableDnsHostnames !== null && this.updateFields.enableDnsHostnames !== this.vpc.enableDnsHostnames) {
      body['enableDnsHostnames'] = this.updateFields.enableDnsHostnames;
    }
    if (this.updateFields.amazonProvidedIpv6CidrBlock !== null) {
      body['amazonProvidedIpv6CidrBlock'] = this.updateFields.amazonProvidedIpv6CidrBlock;
    }

    if (Object.keys(body).length === 0) {
      this.updateError = 'No fields to update. Please modify at least one field.';
      return;
    }

    this.updating = true;
    this.updateError = null;
    this.updateSuccess = null;

    this.cloudServicesService.updateVpc(
      this.vpc.id,
      this.vpc.cloudAccountId,
      body as UpdateVpcRequest
    ).subscribe({
      next: (updated) => {
        this.vpc = updated;
        this.updating = false;
        this.updateSuccess = 'VPC updated successfully.';
        setTimeout(() => this.updateSuccess = null, 4000);
      },
      error: (err) => {
        this.updating = false;
        this.updateError = err?.error?.message || err?.error?.title || 'Failed to update VPC. Please try again.';
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

  deleteVpc(): void {
    if (!this.vpc || this.deleting) return;

    this.deleting = true;
    this.deleteError = null;

    this.cloudServicesService.deleteVpc(
      this.vpc.id,
      this.vpc.cloudAccountId
    ).subscribe({
      next: () => {
        this.deleting = false;
        this.router.navigate(['/dashboard/vpcs']);
      },
      error: (err) => {
        this.deleting = false;
        this.deleteError = err?.error?.message || err?.error?.title || 'Failed to delete VPC. Please try again.';
      }
    });
  }
}
