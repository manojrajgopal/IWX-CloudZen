import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Cluster, UpdateClusterRequest } from '../../../../models/cloud-services.model';

interface InfoItem {
  label: string;
  value: string;
  mono?: boolean;
  copyable?: boolean;
}

@Component({
  selector: 'app-cluster-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cluster-overview.component.html',
  styleUrls: ['./cluster-overview.component.css']
})
export class ClusterOverviewComponent implements OnInit, OnDestroy {
  readonly JSON = JSON;
  loading = true;
  error: string | null = null;

  cluster: Cluster | null = null;
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
  updateFields: { enableContainerInsights: boolean | null } = {
    enableContainerInsights: null
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
      this.error = 'Invalid cluster ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(clusterId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findCluster(accounts, clusterId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findCluster(accounts: CloudAccount[], clusterId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getClusters(a.id).pipe(catchError(() => of({ clusters: [] })))
    );

    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const allClusters: Cluster[] = results.flatMap((r: any) => r.clusters || []);
        this.cluster = allClusters.find(c => c.id === clusterId) || null;

        if (this.cluster) {
          this.account = accounts.find(a => a.id === this.cluster!.cloudAccountId) || null;
        } else {
          this.error = 'Cluster not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load cluster data';
        this.loading = false;
      }
    });
  }

  refreshData(): void {
    if (this.cluster) {
      this.loadData(this.cluster.id);
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

  // ── Helpers ──

  getStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'available'].includes(s)) return 'status-active';
    if (['pending', 'creating', 'updating', 'provisioning'].includes(s)) return 'status-pending';
    if (['deleted', 'terminated', 'inactive', 'error', 'failed'].includes(s)) return 'status-error';
    return 'status-unknown';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'available'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating', 'provisioning'].includes(s)) return 'bg-yellow-500';
    if (['deleted', 'terminated', 'inactive', 'error', 'failed'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  getStatusBgClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'available'].includes(s)) return 'bg-green-50 border-green-200';
    if (['pending', 'creating', 'updating', 'provisioning'].includes(s)) return 'bg-yellow-50 border-yellow-200';
    if (['deleted', 'terminated', 'inactive', 'error', 'failed'].includes(s)) return 'bg-red-50 border-red-200';
    return 'bg-gray-50 border-gray-200';
  }

  getStatusTextClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'running', 'available'].includes(s)) return 'text-green-700';
    if (['pending', 'creating', 'updating', 'provisioning'].includes(s)) return 'text-yellow-700';
    if (['deleted', 'terminated', 'inactive', 'error', 'failed'].includes(s)) return 'text-red-700';
    return 'text-gray-600';
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS ECS/EKS';
      case 'AZURE': return 'Azure AKS';
      case 'GCP': return 'GCP GKE';
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

  // ── Update ──

  openUpdatePanel(): void {
    this.showUpdatePanel = true;
    this.updateError = null;
    this.updateSuccess = null;
    this.updateFields = {
      enableContainerInsights: this.cluster?.containerInsightsEnabled ?? null
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

  updateCluster(): void {
    if (!this.cluster || this.updating) return;

    const body: Record<string, any> = {};
    if (this.updateFields.enableContainerInsights !== null) {
      body['enableContainerInsights'] = this.updateFields.enableContainerInsights;
    }

    if (Object.keys(body).length === 0) {
      this.updateError = 'No fields to update. Please modify at least one field.';
      return;
    }

    this.updating = true;
    this.updateError = null;
    this.updateSuccess = null;

    this.cloudServicesService.updateCluster(
      this.cluster.id,
      this.cluster.cloudAccountId,
      body as UpdateClusterRequest
    ).subscribe({
      next: (updated) => {
        this.cluster = updated;
        this.updating = false;
        this.updateSuccess = 'Cluster updated successfully.';
        setTimeout(() => this.updateSuccess = null, 4000);
      },
      error: (err) => {
        this.updating = false;
        this.updateError = err?.error?.message || err?.error?.title || 'Failed to update cluster. Please try again.';
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

  deleteCluster(): void {
    if (!this.cluster || this.deleting) return;

    this.deleting = true;
    this.deleteError = null;

    this.cloudServicesService.deleteCluster(
      this.cluster.id,
      this.cluster.cloudAccountId
    ).subscribe({
      next: () => {
        this.deleting = false;
        this.router.navigate(['/dashboard/clusters']);
      },
      error: (err) => {
        this.deleting = false;
        this.deleteError = err?.error?.message || err?.error?.title || 'Failed to delete cluster. Please try again.';
      }
    });
  }
}
