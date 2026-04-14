import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Ec2Instance, SecurityGroup, UpdateEc2InstanceRequest } from '../../../../models/cloud-services.model';

@Component({
  selector: 'app-ec2-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ec2-overview.component.html',
  styleUrls: ['./ec2-overview.component.css']
})
export class Ec2OverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  instance: Ec2Instance | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];

  // Section collapse states
  collapsedSections: Record<string, boolean> = {};

  // Copy tooltip
  copiedField: string | null = null;
  private copiedTimeout: any;

  // Update panel
  showUpdatePanel = false;
  updateForm = {
    instanceName: '',
    platform: '',
    securityGroupIds: [] as string[],
    tags: {} as { [key: string]: string }
  };
  updating = false;
  updateMessage: string | null = null;
  updateMessageType: 'success' | 'error' | null = null;
  newTagKey = '';
  newTagValue = '';

  // Available security groups for the update dropdown
  availableSecurityGroups: SecurityGroup[] = [];
  loadingSecurityGroups = false;

  // Stop dialog
  showStopDialog = false;
  forceStop = false;
  stopping = false;

  // Start
  starting = false;

  // Reboot dialog
  showRebootDialog = false;
  rebooting = false;

  // Terminate dialog
  showTerminateDialog = false;
  terminateConfirmText = '';
  terminating = false;

  // Sync
  syncing = false;

  // Toast
  toastMessage: string | null = null;
  toastType: 'success' | 'error' | null = null;
  private toastTimeout: any;

  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.error = 'Invalid EC2 Instance ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
    if (this.toastTimeout) clearTimeout(this.toastTimeout);
  }

  // ── Data Loading ──

  private loadData(instanceId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findInstance(accounts, instanceId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findInstance(accounts: CloudAccount[], instanceId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getEc2Instances(a.id).pipe(catchError(() => of({ instances: [] })))
    );

    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const allInstances: Ec2Instance[] = results.flatMap((r: any) => r.instances || []);
        this.instance = allInstances.find(i => i.id === instanceId) || null;

        if (this.instance) {
          this.account = accounts.find(a => a.id === this.instance!.cloudAccountId) || null;
        } else {
          this.error = 'EC2 Instance not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load EC2 data';
        this.loading = false;
      }
    });
  }

  refreshData(): void {
    if (this.instance) {
      this.cloudServicesService.getEc2InstanceById(this.instance.id, this.instance.cloudAccountId).subscribe({
        next: (updated) => {
          this.instance = updated;
          this.showToast('Data refreshed.', 'success');
        },
        error: () => this.showToast('Failed to refresh.', 'error')
      });
    }
  }

  // ── Section Toggle ──

  toggleSection(section: string): void {
    this.collapsedSections[section] = !this.collapsedSections[section];
  }

  isSectionCollapsed(section: string): boolean {
    return !!this.collapsedSections[section];
  }

  // ── Copy ──

  copyToClipboard(text: string, field: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedField = field;
      if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
      this.copiedTimeout = setTimeout(() => this.copiedField = null, 2000);
    });
  }

  // ── Instance Actions ──

  startInstance(): void {
    if (!this.instance || this.starting) return;
    this.starting = true;
    this.cloudServicesService.startEc2Instance(this.instance.id, this.instance.cloudAccountId).subscribe({
      next: (res: { message: string }) => {
        this.starting = false;
        this.showToast(res.message || 'Start initiated.', 'success');
        setTimeout(() => this.refreshData(), 2000);
      },
      error: (err: any) => {
        this.starting = false;
        this.showToast(err?.error?.message || 'Failed to start instance.', 'error');
      }
    });
  }

  openStopDialog(): void {
    this.forceStop = false;
    this.showStopDialog = true;
  }

  closeStopDialog(): void {
    this.showStopDialog = false;
  }

  confirmStop(): void {
    if (!this.instance || this.stopping) return;
    this.stopping = true;
    this.cloudServicesService.stopEc2Instance(this.instance.id, this.instance.cloudAccountId, this.forceStop).subscribe({
      next: (res: { message: string }) => {
        this.stopping = false;
        this.showStopDialog = false;
        this.showToast(res.message || 'Stop initiated.', 'success');
        setTimeout(() => this.refreshData(), 2000);
      },
      error: (err: any) => {
        this.stopping = false;
        this.showToast(err?.error?.message || 'Failed to stop instance.', 'error');
      }
    });
  }

  openRebootDialog(): void {
    this.showRebootDialog = true;
  }

  closeRebootDialog(): void {
    this.showRebootDialog = false;
  }

  confirmReboot(): void {
    if (!this.instance || this.rebooting) return;
    this.rebooting = true;
    this.cloudServicesService.rebootEc2Instance(this.instance.id, this.instance.cloudAccountId).subscribe({
      next: (res: { message: string }) => {
        this.rebooting = false;
        this.showRebootDialog = false;
        this.showToast(res.message || 'Reboot initiated.', 'success');
        setTimeout(() => this.refreshData(), 3000);
      },
      error: (err: any) => {
        this.rebooting = false;
        this.showToast(err?.error?.message || 'Failed to reboot instance.', 'error');
      }
    });
  }

  openTerminateDialog(): void {
    this.terminateConfirmText = '';
    this.showTerminateDialog = true;
  }

  closeTerminateDialog(): void {
    this.showTerminateDialog = false;
    this.terminateConfirmText = '';
  }

  confirmTerminate(): void {
    if (!this.instance || this.terminating) return;
    this.terminating = true;
    this.cloudServicesService.terminateEc2Instance(this.instance.id, this.instance.cloudAccountId).subscribe({
      next: () => {
        this.terminating = false;
        this.showTerminateDialog = false;
        this.showToast('Instance terminated successfully.', 'success');
        setTimeout(() => this.router.navigate(['/dashboard/ec2-instances']), 1500);
      },
      error: (err: any) => {
        this.terminating = false;
        this.showToast(err?.error?.message || 'Failed to terminate instance.', 'error');
      }
    });
  }

  // ── Update ──

  openUpdatePanel(): void {
    if (!this.instance) return;
    this.updateForm = {
      instanceName: this.instance.name || '',
      platform: this.instance.platform || '',
      securityGroupIds: this.instance.securityGroups?.map(sg => sg.groupId) || [],
      tags: { ...(this.instance.tags || {}) }
    };
    this.newTagKey = '';
    this.newTagValue = '';
    this.showUpdatePanel = true;
    this.updateMessage = null;
    this.loadSecurityGroupsForUpdate();
  }

  closeUpdatePanel(): void {
    this.showUpdatePanel = false;
    this.updateMessage = null;
  }

  private loadSecurityGroupsForUpdate(): void {
    if (!this.instance) return;
    this.loadingSecurityGroups = true;
    this.cloudServicesService.getSecurityGroups(this.instance.cloudAccountId).subscribe({
      next: (res: any) => {
        this.availableSecurityGroups = res.securityGroups || [];
        this.loadingSecurityGroups = false;
      },
      error: () => {
        this.availableSecurityGroups = [];
        this.loadingSecurityGroups = false;
      }
    });
  }

  toggleSecurityGroup(groupId: string): void {
    const idx = this.updateForm.securityGroupIds.indexOf(groupId);
    if (idx >= 0) {
      this.updateForm.securityGroupIds.splice(idx, 1);
    } else {
      this.updateForm.securityGroupIds.push(groupId);
    }
  }

  isSecurityGroupSelected(groupId: string): boolean {
    return this.updateForm.securityGroupIds.includes(groupId);
  }

  addTag(): void {
    const key = this.newTagKey.trim();
    const value = this.newTagValue.trim();
    if (key) {
      this.updateForm.tags[key] = value;
      this.newTagKey = '';
      this.newTagValue = '';
    }
  }

  removeTag(key: string): void {
    delete this.updateForm.tags[key];
  }

  get updateTagEntries(): { key: string; value: string }[] {
    return Object.entries(this.updateForm.tags).map(([key, value]) => ({ key, value }));
  }

  submitUpdate(): void {
    if (!this.instance || this.updating) return;
    this.updating = true;
    this.updateMessage = null;

    const request: UpdateEc2InstanceRequest = {
      instanceName: this.updateForm.instanceName,
      platform: this.updateForm.platform,
      securityGroupIds: this.updateForm.securityGroupIds,
      tags: this.updateForm.tags
    };

    this.cloudServicesService.updateEc2Instance(this.instance.id, this.instance.cloudAccountId, request).subscribe({
      next: (updated: Ec2Instance) => {
        this.instance = updated;
        this.updating = false;
        this.showUpdatePanel = false;
        this.showToast('Instance updated successfully.', 'success');
      },
      error: (err: any) => {
        this.updating = false;
        this.updateMessage = err?.error?.message || 'Failed to update instance.';
        this.updateMessageType = 'error';
      }
    });
  }

  // ── Sync ──

  syncInstance(): void {
    if (!this.instance || this.syncing) return;
    this.syncing = true;
    this.cloudServicesService.syncEc2Instances(this.instance.cloudAccountId).subscribe({
      next: (result: any) => {
        this.syncing = false;
        this.showToast(`Sync complete — ${result.added} added, ${result.updated} updated, ${result.removed} removed`, 'success');
        this.refreshData();
      },
      error: () => {
        this.syncing = false;
        this.showToast('Sync failed.', 'error');
      }
    });
  }

  // ── Display Helpers ──

  getStateBgClass(state: string): string {
    const s = state?.toLowerCase();
    if (s === 'running') return 'bg-green-50 border-green-200';
    if (s === 'stopped') return 'bg-red-50 border-red-200';
    if (['pending', 'stopping', 'shutting-down'].includes(s)) return 'bg-yellow-50 border-yellow-200';
    if (s === 'terminated') return 'bg-gray-100 border-gray-300';
    return 'bg-gray-50 border-gray-200';
  }

  getStateTextClass(state: string): string {
    const s = state?.toLowerCase();
    if (s === 'running') return 'text-green-700';
    if (s === 'stopped') return 'text-red-700';
    if (['pending', 'stopping', 'shutting-down'].includes(s)) return 'text-yellow-700';
    if (s === 'terminated') return 'text-gray-600';
    return 'text-gray-600';
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

  getMonitoringClass(monitoring: string): string {
    if (monitoring?.toLowerCase() === 'enabled') return 'bg-green-50 text-green-700 border-green-200';
    return 'bg-gray-50 text-gray-500 border-gray-200';
  }

  get isRunning(): boolean {
    return this.instance?.state?.toLowerCase() === 'running';
  }

  get isStopped(): boolean {
    return this.instance?.state?.toLowerCase() === 'stopped';
  }

  get isTerminated(): boolean {
    return this.instance?.state?.toLowerCase() === 'terminated';
  }

  get isTransitioning(): boolean {
    const s = this.instance?.state?.toLowerCase();
    return ['pending', 'stopping', 'shutting-down'].includes(s || '');
  }

  get tagEntries(): { key: string; value: string }[] {
    if (!this.instance?.tags) return [];
    return Object.entries(this.instance.tags).map(([key, value]) => ({ key, value }));
  }

  get securityGroupCount(): number {
    return this.instance?.securityGroups?.length || 0;
  }

  get tagCount(): number {
    return this.tagEntries.length;
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

  // ── Toast ──

  private showToast(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    if (this.toastTimeout) clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => this.toastMessage = null, 5000);
  }
}
