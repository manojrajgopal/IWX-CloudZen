import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { SecurityGroup, CreateSecurityGroupRequest, CreateSecurityGroupRuleRequest, Vpc } from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-security-group',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-security-group.component.html',
  styleUrls: ['./create-security-group.component.css']
})
export class CreateSecurityGroupComponent implements OnInit {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // VPC selector
  vpcs: Vpc[] = [];
  selectedVpc: Vpc | null = null;
  vpcsLoading = false;
  vpcsError: string | null = null;

  groupName = '';
  description = '';

  inboundRules: CreateSecurityGroupRuleRequest[] = [];
  outboundRules: CreateSecurityGroupRuleRequest[] = [];

  formState: FormState = 'loading';
  createdGroup: SecurityGroup | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;

  // Touched flags
  groupNameTouched = false;
  descriptionTouched = false;

  constructor(
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.returnTo = this.route.snapshot.queryParamMap.get('returnTo');
    this.loadAccounts();
  }

  private loadAccounts(): void {
    this.formState = 'loading';
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts.filter(a => a.provider?.toUpperCase() === 'AWS');

        // Pre-select account if returning from create-vpc
        const preSelectId = this.route.snapshot.queryParamMap.get('accountId');
        if (preSelectId) {
          const found = this.accounts.find(a => a.id === +preSelectId);
          if (found) this.selectedAccountId = found.id;
        } else if (this.accounts.length === 1) {
          this.selectedAccountId = this.accounts[0].id;
        }

        // Default inbound + outbound rule
        this.addInboundRule();
        this.addOutboundRule();
        this.formState = 'form';
        this.cdr.detectChanges();

        // Auto-load VPCs if account is pre-selected
        if (this.selectedAccountId) {
          this.loadVpcs(this.selectedAccountId);
        }
      },
      error: () => {
        this.errorMessage = 'Failed to load cloud accounts. Please try again.';
        this.formState = 'error';
      }
    });
  }

  selectAccount(accountId: number): void {
    this.selectedAccountId = accountId;
    this.selectedVpc = null;
    this.vpcs = [];
    this.vpcsError = null;
    this.loadVpcs(accountId);
  }

  loadVpcs(accountId: number): void {
    this.vpcsLoading = true;
    this.vpcsError = null;
    this.cloudServicesService.getVpcs(accountId).subscribe({
      next: (res) => {
        this.vpcs = res.vpcs || [];
        this.vpcsLoading = false;
        // Re-select previously selected VPC after reload if it still exists
        if (this.selectedVpc) {
          this.selectedVpc = this.vpcs.find(v => v.id === this.selectedVpc!.id) || null;
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.vpcsError = 'Failed to load VPCs. Please try again.';
        this.vpcsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectVpc(vpc: Vpc): void {
    this.selectedVpc = vpc;
  }

  getVpcDisplayName(vpc: Vpc): string {
    return vpc.name?.trim() ? vpc.name.trim() : vpc.vpcId;
  }

  navigateToCreateVpc(): void {
    let returnPath = '/dashboard/security-groups/create';
    const qp: string[] = [];
    if (this.returnTo) qp.push(`returnTo=${encodeURIComponent(this.returnTo)}`);
    if (this.selectedAccountId) qp.push(`accountId=${this.selectedAccountId}`);
    if (qp.length) returnPath += '?' + qp.join('&');

    const params: any = { returnTo: returnPath };
    if (this.selectedAccountId) params['accountId'] = this.selectedAccountId;
    this.router.navigate(['/dashboard/vpcs/create'], { queryParams: params });
  }

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      this.groupName.trim().length >= 1 &&
      this.description.trim().length >= 1 &&
      !!this.selectedVpc;
  }

  get groupNameError(): string | null {
    if (!this.groupNameTouched) return null;
    if (!this.groupName.trim()) return 'Group name is required';
    if (this.groupName.trim().length > 255) return 'Group name must be 255 characters or less';
    return null;
  }

  get descriptionError(): string | null {
    if (!this.descriptionTouched) return null;
    if (!this.description.trim()) return 'Description is required';
    return null;
  }

  // ── Inbound Rules ──

  addInboundRule(): void {
    this.inboundRules.push({
      protocol: 'tcp',
      fromPort: 80,
      toPort: 80,
      ipv4Ranges: ['0.0.0.0/0'],
      ipv6Ranges: [],
      referencedGroupIds: [],
      description: null
    });
  }

  removeInboundRule(index: number): void {
    this.inboundRules.splice(index, 1);
  }

  getInboundIpv4(rule: CreateSecurityGroupRuleRequest): string {
    return rule.ipv4Ranges.join(', ');
  }

  setInboundIpv4(rule: CreateSecurityGroupRuleRequest, value: string): void {
    rule.ipv4Ranges = value.split(',').map(s => s.trim()).filter(Boolean);
  }

  getInboundIpv6(rule: CreateSecurityGroupRuleRequest): string {
    return rule.ipv6Ranges.join(', ');
  }

  setInboundIpv6(rule: CreateSecurityGroupRuleRequest, value: string): void {
    rule.ipv6Ranges = value.split(',').map(s => s.trim()).filter(Boolean);
  }

  // ── Outbound Rules ──

  addOutboundRule(): void {
    this.outboundRules.push({
      protocol: '-1',
      fromPort: -1,
      toPort: -1,
      ipv4Ranges: ['0.0.0.0/0'],
      ipv6Ranges: [],
      referencedGroupIds: [],
      description: null
    });
  }

  removeOutboundRule(index: number): void {
    this.outboundRules.splice(index, 1);
  }

  getOutboundIpv4(rule: CreateSecurityGroupRuleRequest): string {
    return rule.ipv4Ranges.join(', ');
  }

  setOutboundIpv4(rule: CreateSecurityGroupRuleRequest, value: string): void {
    rule.ipv4Ranges = value.split(',').map(s => s.trim()).filter(Boolean);
  }

  getOutboundIpv6(rule: CreateSecurityGroupRuleRequest): string {
    return rule.ipv6Ranges.join(', ');
  }

  setOutboundIpv6(rule: CreateSecurityGroupRuleRequest, value: string): void {
    rule.ipv6Ranges = value.split(',').map(s => s.trim()).filter(Boolean);
  }

  isAllTraffic(rule: CreateSecurityGroupRuleRequest): boolean {
    return rule.protocol === '-1';
  }

  // ── Submit ──

  create(): void {
    if (!this.isFormValid || !this.selectedAccountId || !this.selectedVpc) return;

    const request: CreateSecurityGroupRequest = {
      groupName: this.groupName.trim(),
      description: this.description.trim(),
      vpcId: this.selectedVpc.vpcId,
      inboundRules: this.inboundRules,
      outboundRules: this.outboundRules
    };

    this.formState = 'creating';
    this.progress = 0;
    this.errorMessage = '';

    this.progressInterval = setInterval(() => {
      if (this.progress < 85) {
        this.progress += Math.random() * 8 + 2;
        this.progress = Math.min(this.progress, 85);
        this.cdr.detectChanges();
      }
    }, 150);

    this.cloudServicesService.createSecurityGroup(this.selectedAccountId, request).subscribe({
      next: (group) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdGroup = group;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create security group. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.groupName = '';
    this.description = '';
    this.selectedVpc = null;
    this.inboundRules = [];
    this.outboundRules = [];
    this.groupNameTouched = false;
    this.descriptionTouched = false;
    this.createdGroup = null;
    this.errorMessage = '';
    this.addInboundRule();
    this.addOutboundRule();
    this.formState = 'form';
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/security-groups']);
    }
  }

  goToDashboard(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/security-groups']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to Security Groups';
    const segments = this.returnTo.replace(/^\//, '').split('/').filter(s => s && s !== 'dashboard');
    if (segments.length === 0) return 'Back';
    const label = segments
      .map(s => s.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '))
      .join(' \u203a ');
    return `Back to ${label}`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  getProtocolLabel(protocol: string): string {
    switch (protocol) {
      case '-1': return 'All Traffic';
      case 'tcp': return 'TCP';
      case 'udp': return 'UDP';
      case 'icmp': return 'ICMP';
      default: return protocol.toUpperCase();
    }
  }
}
