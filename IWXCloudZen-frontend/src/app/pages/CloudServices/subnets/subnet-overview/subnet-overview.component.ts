import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Subnet, Vpc, UpdateSubnetRequest } from '../../../../models/cloud-services.model';

@Component({
  selector: 'app-subnet-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './subnet-overview.component.html',
  styleUrls: ['./subnet-overview.component.css']
})
export class SubnetOverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  subnet: Subnet | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];

  // VPC info
  parentVpc: Vpc | null = null;

  // Related subnets in same VPC
  relatedSubnets: Subnet[] = [];
  loadingRelated = false;

  // VPC filter for related subnets
  vpcs: Vpc[] = [];
  selectedVpcFilter = '';
  loadingVpcs = false;

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
    name: string | null;
    mapPublicIpOnLaunch: boolean | null;
    assignIpv6AddressOnCreation: boolean | null;
  } = {
    name: null,
    mapPublicIpOnLaunch: null,
    assignIpv6AddressOnCreation: null
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
      this.error = 'Invalid Subnet ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(subnetId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findSubnet(accounts, subnetId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findSubnet(accounts: CloudAccount[], subnetId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getSubnets(a.id).pipe(catchError(() => of({ subnets: [] })))
    );

    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const allSubnets: Subnet[] = results.flatMap((r: any) => r.subnets || []);
        this.subnet = allSubnets.find(s => s.id === subnetId) || null;

        if (this.subnet) {
          this.account = accounts.find(a => a.id === this.subnet!.cloudAccountId) || null;
          this.loadVpcs();
          this.loadRelatedSubnets();
        } else {
          this.error = 'Subnet not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load subnet data';
        this.loading = false;
      }
    });
  }

  private loadVpcs(): void {
    if (!this.subnet) return;
    this.loadingVpcs = true;
    this.cloudServicesService.getVpcs(this.subnet.cloudAccountId).pipe(
      catchError(() => of({ vpcs: [] }))
    ).subscribe({
      next: (res: any) => {
        this.vpcs = res.vpcs || [];
        this.parentVpc = this.vpcs.find(v => v.vpcId === this.subnet!.vpcId) || null;
        this.selectedVpcFilter = this.subnet!.vpcId;
        this.loadingVpcs = false;
      },
      error: () => {
        this.loadingVpcs = false;
      }
    });
  }

  private loadRelatedSubnets(): void {
    if (!this.subnet) return;
    this.loadingRelated = true;
    this.cloudServicesService.getSubnetsByVpc(this.subnet.cloudAccountId, this.subnet.vpcId).pipe(
      catchError(() => of({ subnets: [] }))
    ).subscribe({
      next: (res: any) => {
        this.relatedSubnets = (res.subnets || []).filter((s: Subnet) => s.id !== this.subnet!.id);
        this.loadingRelated = false;
      },
      error: () => {
        this.loadingRelated = false;
      }
    });
  }

  onVpcFilterChange(): void {
    if (!this.subnet) return;
    this.loadingRelated = true;
    this.cloudServicesService.getSubnetsByVpc(this.subnet.cloudAccountId, this.selectedVpcFilter).pipe(
      catchError(() => of({ subnets: [] }))
    ).subscribe({
      next: (res: any) => {
        this.relatedSubnets = (res.subnets || []).filter((s: Subnet) => s.id !== this.subnet!.id);
        this.loadingRelated = false;
      },
      error: () => {
        this.loadingRelated = false;
      }
    });
  }

  refreshData(): void {
    if (this.subnet) {
      this.loadData(this.subnet.id);
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

  navigateToSubnet(subnetId: number): void {
    this.router.navigate(['/dashboard/subnets', subnetId]);
    setTimeout(() => {
      this.loading = true;
      this.error = null;
      this.loadData(subnetId);
    }, 50);
  }

  navigateToVpc(): void {
    if (this.parentVpc) {
      this.router.navigate(['/dashboard/vpcs', this.parentVpc.id]);
    }
  }

  // ── Display Helpers ──

  getDisplayName(subnet: Subnet): string {
    return subnet.name || subnet.subnetId || '—';
  }

  getSubnetType(subnet: Subnet): string {
    return subnet.mapPublicIpOnLaunch ? 'Public' : 'Private';
  }

  getSubnetTypeBgClass(subnet: Subnet): string {
    return subnet.mapPublicIpOnLaunch
      ? 'bg-blue-50 border-blue-200 text-blue-700'
      : 'bg-purple-50 border-purple-200 text-purple-700';
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

  getIpUtilization(): number {
    if (!this.subnet) return 0;
    const totalIps = Math.pow(2, 32 - parseInt(this.subnet.cidrBlock.split('/')[1], 10));
    // AWS reserves 5 IPs per subnet
    const usable = totalIps - 5;
    const used = usable - this.subnet.availableIpAddressCount;
    return usable > 0 ? Math.round((used / usable) * 100) : 0;
  }

  getUsedIpCount(): number {
    if (!this.subnet) return 0;
    const totalIps = Math.pow(2, 32 - parseInt(this.subnet.cidrBlock.split('/')[1], 10));
    const usable = totalIps - 5;
    return Math.max(0, usable - this.subnet.availableIpAddressCount);
  }

  getUsableIpCount(): number {
    if (!this.subnet) return 0;
    const totalIps = Math.pow(2, 32 - parseInt(this.subnet.cidrBlock.split('/')[1], 10));
    return totalIps - 5; // AWS reserves 5 IPs per subnet
  }

  getVpcDisplayName(vpc: Vpc): string {
    return vpc.name || vpc.vpcId;
  }

  // ── Update ──

  openUpdatePanel(): void {
    this.showUpdatePanel = true;
    this.updateError = null;
    this.updateSuccess = null;
    this.updateFields = {
      name: this.subnet?.name ?? null,
      mapPublicIpOnLaunch: this.subnet?.mapPublicIpOnLaunch ?? null,
      assignIpv6AddressOnCreation: this.subnet?.assignIpv6AddressOnCreation ?? null
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

  updateSubnet(): void {
    if (!this.subnet || this.updating) return;

    const body: Record<string, any> = {};
    if (this.updateFields.name !== null && this.updateFields.name !== this.subnet.name) {
      body['name'] = this.updateFields.name;
    }
    if (this.updateFields.mapPublicIpOnLaunch !== null && this.updateFields.mapPublicIpOnLaunch !== this.subnet.mapPublicIpOnLaunch) {
      body['mapPublicIpOnLaunch'] = this.updateFields.mapPublicIpOnLaunch;
    }
    if (this.updateFields.assignIpv6AddressOnCreation !== null && this.updateFields.assignIpv6AddressOnCreation !== this.subnet.assignIpv6AddressOnCreation) {
      body['assignIpv6AddressOnCreation'] = this.updateFields.assignIpv6AddressOnCreation;
    }

    if (Object.keys(body).length === 0) {
      this.updateError = 'No fields to update. Please modify at least one field.';
      return;
    }

    this.updating = true;
    this.updateError = null;
    this.updateSuccess = null;

    this.cloudServicesService.updateSubnet(
      this.subnet.id,
      this.subnet.cloudAccountId,
      body as UpdateSubnetRequest
    ).subscribe({
      next: (updated) => {
        this.subnet = updated;
        this.updating = false;
        this.updateSuccess = 'Subnet updated successfully.';
        setTimeout(() => this.updateSuccess = null, 4000);
      },
      error: (err) => {
        this.updating = false;
        this.updateError = err?.error?.message || err?.error?.title || 'Failed to update subnet. Please try again.';
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

  deleteSubnet(): void {
    if (!this.subnet || this.deleting) return;

    this.deleting = true;
    this.deleteError = null;

    this.cloudServicesService.deleteSubnet(
      this.subnet.id,
      this.subnet.cloudAccountId
    ).subscribe({
      next: () => {
        this.deleting = false;
        this.router.navigate(['/dashboard/subnets']);
      },
      error: (err) => {
        this.deleting = false;
        this.deleteError = err?.error?.message || err?.error?.title || 'Failed to delete subnet. Please try again.';
      }
    });
  }
}
