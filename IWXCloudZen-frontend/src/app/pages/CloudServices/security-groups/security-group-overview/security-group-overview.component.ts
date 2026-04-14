import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { SecurityGroup, SecurityGroupRule, CreateSecurityGroupRuleRequest } from '../../../../models/cloud-services.model';

@Component({
  selector: 'app-security-group-overview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './security-group-overview.component.html',
  styleUrls: ['./security-group-overview.component.css']
})
export class SecurityGroupOverviewComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  sg: SecurityGroup | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];

  // Related security groups in the same VPC
  relatedGroups: SecurityGroup[] = [];
  loadingRelated = false;

  // Section collapse states
  collapsedSections: Record<string, boolean> = {};

  // Active rules tab
  activeRulesTab: 'inbound' | 'outbound' = 'inbound';

  // Copied tooltip
  copiedField: string | null = null;
  private copiedTimeout: any;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.error = 'Invalid Security Group ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(sgId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.findSecurityGroup(accounts, sgId);
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findSecurityGroup(accounts: CloudAccount[], sgId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }

    const requests = accounts.map(a =>
      this.cloudServicesService.getSecurityGroups(a.id).pipe(catchError(() => of({ securityGroups: [] })))
    );

    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const allGroups: SecurityGroup[] = results.flatMap((r: any) => r.securityGroups || []);
        this.sg = allGroups.find(g => g.id === sgId) || null;

        if (this.sg) {
          this.account = accounts.find(a => a.id === this.sg!.cloudAccountId) || null;
          this.loadRelatedGroups();
        } else {
          this.error = 'Security Group not found';
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load security group data';
        this.loading = false;
      }
    });
  }

  private loadRelatedGroups(): void {
    if (!this.sg) return;
    this.loadingRelated = true;
    this.cloudServicesService.getSecurityGroups(this.sg.cloudAccountId).pipe(
      catchError(() => of({ securityGroups: [] }))
    ).subscribe({
      next: (res: any) => {
        this.relatedGroups = (res.securityGroups || [])
          .filter((g: SecurityGroup) => g.vpcId === this.sg!.vpcId && g.id !== this.sg!.id);
        this.loadingRelated = false;
      },
      error: () => {
        this.loadingRelated = false;
      }
    });
  }

  refreshData(): void {
    if (this.sg) {
      this.loadData(this.sg.id);
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

  navigateToGroup(sgId: number): void {
    this.router.navigate(['/dashboard/security-groups', sgId]);
    setTimeout(() => {
      this.loading = true;
      this.error = null;
      this.loadData(sgId);
    }, 50);
  }

  // ── Display Helpers ──

  getDisplayName(sg: SecurityGroup): string {
    return sg.groupName || sg.securityGroupId || '—';
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

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatDateTime(dateStr: string | null): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
  }

  formatRelativeTime(dateStr: string | null): string {
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

  // ── Rule Helpers ──

  getProtocolLabel(protocol: string): string {
    if (protocol === '-1') return 'All Traffic';
    return protocol?.toUpperCase() || '—';
  }

  getPortRange(rule: SecurityGroupRule): string {
    if (rule.protocol === '-1') return 'All';
    if (rule.fromPort === rule.toPort) return rule.fromPort.toString();
    return `${rule.fromPort}–${rule.toPort}`;
  }

  getPortLabel(rule: SecurityGroupRule): string {
    if (rule.protocol === '-1') return 'All Ports';
    const port = rule.fromPort;
    switch (port) {
      case 22: return 'SSH';
      case 80: return 'HTTP';
      case 443: return 'HTTPS';
      case 3306: return 'MySQL';
      case 5432: return 'PostgreSQL';
      case 3389: return 'RDP';
      case 8080: return 'HTTP Alt';
      case 8443: return 'HTTPS Alt';
      case 27017: return 'MongoDB';
      case 6379: return 'Redis';
      default: return '';
    }
  }

  getSourceDisplay(rule: SecurityGroupRule): string[] {
    const sources: string[] = [];
    if (rule.ipv4Ranges?.length) sources.push(...rule.ipv4Ranges);
    if (rule.ipv6Ranges?.length) sources.push(...rule.ipv6Ranges);
    if (rule.referencedGroupIds?.length) sources.push(...rule.referencedGroupIds);
    return sources.length > 0 ? sources : ['—'];
  }

  isOpenToAll(rule: SecurityGroupRule): boolean {
    return rule.ipv4Ranges?.includes('0.0.0.0/0') || rule.ipv6Ranges?.includes('::/0');
  }

  getRuleRiskLevel(rule: SecurityGroupRule): 'high' | 'medium' | 'low' {
    if (this.isOpenToAll(rule)) {
      if (rule.protocol === '-1') return 'high';
      if ([22, 3389].includes(rule.fromPort)) return 'high';
      if ([3306, 5432, 27017, 6379].includes(rule.fromPort)) return 'high';
      return 'medium';
    }
    return 'low';
  }

  getRiskBadgeClass(risk: 'high' | 'medium' | 'low'): string {
    switch (risk) {
      case 'high': return 'bg-red-50 border-red-200 text-red-700';
      case 'medium': return 'bg-yellow-50 border-yellow-200 text-yellow-700';
      case 'low': return 'bg-green-50 border-green-200 text-green-700';
    }
  }

  getRiskLabel(risk: 'high' | 'medium' | 'low'): string {
    switch (risk) {
      case 'high': return 'High Risk';
      case 'medium': return 'Medium';
      case 'low': return 'Low Risk';
    }
  }

  getProtocolBgClass(protocol: string): string {
    if (protocol === '-1') return 'bg-purple-50 border-purple-200 text-purple-700';
    switch (protocol?.toLowerCase()) {
      case 'tcp': return 'bg-blue-50 border-blue-200 text-blue-700';
      case 'udp': return 'bg-teal-50 border-teal-200 text-teal-700';
      case 'icmp': return 'bg-orange-50 border-orange-200 text-orange-700';
      default: return 'bg-gray-50 border-gray-200 text-gray-600';
    }
  }

  // ── Metrics computed from rules ──

  get totalInboundRules(): number { return this.sg?.inboundRules?.length || 0; }
  get totalOutboundRules(): number { return this.sg?.outboundRules?.length || 0; }
  get totalRules(): number { return this.totalInboundRules + this.totalOutboundRules; }

  get highRiskRuleCount(): number {
    if (!this.sg) return 0;
    const allRules = [...(this.sg.inboundRules || []), ...(this.sg.outboundRules || [])];
    return allRules.filter(r => this.getRuleRiskLevel(r) === 'high').length;
  }

  get uniqueProtocols(): string[] {
    if (!this.sg) return [];
    const allRules = [...(this.sg.inboundRules || []), ...(this.sg.outboundRules || [])];
    return [...new Set(allRules.map(r => this.getProtocolLabel(r.protocol)))];
  }

  get uniquePorts(): number[] {
    if (!this.sg) return [];
    const allRules = [...(this.sg.inboundRules || []), ...(this.sg.outboundRules || [])];
    const ports = new Set<number>();
    allRules.forEach(r => {
      if (r.protocol !== '-1') {
        if (r.fromPort === r.toPort) {
          ports.add(r.fromPort);
        } else {
          ports.add(r.fromPort);
          ports.add(r.toPort);
        }
      }
    });
    return [...ports].sort((a, b) => a - b);
  }

  get allOpenSources(): string[] {
    if (!this.sg) return [];
    const allRules = [...(this.sg.inboundRules || []), ...(this.sg.outboundRules || [])];
    const sources = new Set<string>();
    allRules.forEach(r => {
      r.ipv4Ranges?.forEach(s => sources.add(s));
      r.ipv6Ranges?.forEach(s => sources.add(s));
    });
    return [...sources];
  }

  get securityScore(): number {
    if (!this.sg) return 100;
    const allRules = [...(this.sg.inboundRules || [])];
    if (allRules.length === 0) return 100;
    let score = 100;
    allRules.forEach(r => {
      const risk = this.getRuleRiskLevel(r);
      if (risk === 'high') score -= 25;
      else if (risk === 'medium') score -= 10;
    });
    return Math.max(0, score);
  }

  getScoreClass(): string {
    const s = this.securityScore;
    if (s >= 80) return 'text-green-700';
    if (s >= 50) return 'text-yellow-700';
    return 'text-red-700';
  }

  getScoreBgClass(): string {
    const s = this.securityScore;
    if (s >= 80) return 'bg-green-50';
    if (s >= 50) return 'bg-yellow-50';
    return 'bg-red-50';
  }

  getScoreLabel(): string {
    const s = this.securityScore;
    if (s >= 80) return 'Good';
    if (s >= 50) return 'Fair';
    return 'Poor';
  }

  // ══════════════════════════════════════════
  // UPDATE Security Group
  // ══════════════════════════════════════════
  showUpdatePanel = false;
  updating = false;
  updateError: string | null = null;
  updateSuccess: string | null = null;
  updateFields: { groupName: string; description: string } = { groupName: '', description: '' };

  openUpdatePanel(): void {
    this.showUpdatePanel = true;
    this.updateError = null;
    this.updateSuccess = null;
    this.updateFields = {
      groupName: this.sg?.groupName ?? '',
      description: this.sg?.description ?? ''
    };
    setTimeout(() => {
      document.getElementById('updatePanel')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 100);
  }

  closeUpdatePanel(): void {
    this.showUpdatePanel = false;
    this.updateError = null;
    this.updateSuccess = null;
  }

  updateSecurityGroup(): void {
    if (!this.sg || !this.account) return;
    this.updating = true;
    this.updateError = null;
    this.updateSuccess = null;

    const body = {
      groupName: this.updateFields.groupName,
      description: this.updateFields.description,
      vpcId: this.sg.vpcId,
      inboundRules: this.sg.inboundRules.map(r => this.ruleToRequest(r)),
      outboundRules: this.sg.outboundRules.map(r => this.ruleToRequest(r))
    };

    this.cloudServicesService.updateSecurityGroup(this.sg.id, this.account.id, body).subscribe({
      next: (updated) => {
        this.sg = updated;
        this.updateSuccess = 'Security group updated successfully.';
        this.updating = false;
      },
      error: (err) => {
        this.updateError = err?.error?.message || 'Failed to update security group.';
        this.updating = false;
      }
    });
  }

  private ruleToRequest(rule: SecurityGroupRule): CreateSecurityGroupRuleRequest {
    return {
      protocol: rule.protocol,
      fromPort: rule.fromPort,
      toPort: rule.toPort,
      ipv4Ranges: rule.ipv4Ranges || [],
      ipv6Ranges: rule.ipv6Ranges || [],
      referencedGroupIds: rule.referencedGroupIds || [],
      description: rule.description
    };
  }

  // ══════════════════════════════════════════
  // DELETE Security Group
  // ══════════════════════════════════════════
  showDeleteConfirm = false;
  deleting = false;
  deleteError: string | null = null;

  openDeleteConfirm(): void {
    this.showDeleteConfirm = true;
    this.deleteError = null;
  }

  closeDeleteConfirm(): void {
    this.showDeleteConfirm = false;
    this.deleteError = null;
  }

  deleteSecurityGroup(): void {
    if (!this.sg || !this.account) return;
    this.deleting = true;
    this.deleteError = null;

    this.cloudServicesService.deleteSecurityGroup(this.sg.id, this.account.id).subscribe({
      next: () => {
        this.deleting = false;
        this.router.navigate(['/dashboard/security-groups']);
      },
      error: (err) => {
        this.deleteError = err?.error?.message || 'Failed to delete security group.';
        this.deleting = false;
      }
    });
  }

  // ══════════════════════════════════════════
  // ADD RULES
  // ══════════════════════════════════════════
  showAddRulePanel: 'inbound' | 'outbound' | null = null;
  addingRule = false;
  addRuleError: string | null = null;
  addRuleSuccess: string | null = null;

  // New rule form fields
  newRuleProtocol = 'tcp';
  newRuleFromPort = 80;
  newRuleToPort = 80;
  newRuleDescription = '';
  newRuleIpv4Open = true;
  newRuleIpv6Open = false;
  newRuleCustomCidr = '';

  // Preset templates for quick selection
  rulePresets: { label: string; protocol: string; fromPort: number; toPort: number; description: string }[] = [
    { label: 'SSH', protocol: 'tcp', fromPort: 22, toPort: 22, description: 'Allow SSH' },
    { label: 'HTTP', protocol: 'tcp', fromPort: 80, toPort: 80, description: 'Allow HTTP' },
    { label: 'HTTPS', protocol: 'tcp', fromPort: 443, toPort: 443, description: 'Allow HTTPS' },
    { label: 'RDP', protocol: 'tcp', fromPort: 3389, toPort: 3389, description: 'Allow RDP' },
    { label: 'MySQL', protocol: 'tcp', fromPort: 3306, toPort: 3306, description: 'Allow MySQL' },
    { label: 'PostgreSQL', protocol: 'tcp', fromPort: 5432, toPort: 5432, description: 'Allow PostgreSQL' },
    { label: 'MongoDB', protocol: 'tcp', fromPort: 27017, toPort: 27017, description: 'Allow MongoDB' },
    { label: 'Redis', protocol: 'tcp', fromPort: 6379, toPort: 6379, description: 'Allow Redis' },
    { label: 'HTTP Alt', protocol: 'tcp', fromPort: 8080, toPort: 8080, description: 'Allow HTTP Alt' },
    { label: 'All Traffic', protocol: '-1', fromPort: -1, toPort: -1, description: 'Allow all traffic' },
    { label: 'DNS (UDP)', protocol: 'udp', fromPort: 53, toPort: 53, description: 'Allow DNS' },
    { label: 'ICMP', protocol: 'icmp', fromPort: -1, toPort: -1, description: 'Allow ICMP' },
  ];

  openAddRulePanel(type: 'inbound' | 'outbound'): void {
    this.showAddRulePanel = type;
    this.addRuleError = null;
    this.addRuleSuccess = null;
    this.resetNewRuleForm();
    setTimeout(() => {
      document.getElementById('addRulePanel')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  closeAddRulePanel(): void {
    this.showAddRulePanel = null;
    this.addRuleError = null;
    this.addRuleSuccess = null;
  }

  selectPreset(preset: { label: string; protocol: string; fromPort: number; toPort: number; description: string }): void {
    this.newRuleProtocol = preset.protocol;
    this.newRuleFromPort = preset.fromPort;
    this.newRuleToPort = preset.toPort;
    this.newRuleDescription = preset.description;
  }

  resetNewRuleForm(): void {
    this.newRuleProtocol = 'tcp';
    this.newRuleFromPort = 80;
    this.newRuleToPort = 80;
    this.newRuleDescription = '';
    this.newRuleIpv4Open = true;
    this.newRuleIpv6Open = false;
    this.newRuleCustomCidr = '';
  }

  submitAddRule(): void {
    if (!this.sg || !this.account || !this.showAddRulePanel) return;
    this.addingRule = true;
    this.addRuleError = null;
    this.addRuleSuccess = null;

    const ipv4Ranges: string[] = [];
    const ipv6Ranges: string[] = [];
    if (this.newRuleCustomCidr.trim()) {
      const cidr = this.newRuleCustomCidr.trim();
      if (cidr.includes(':')) ipv6Ranges.push(cidr);
      else ipv4Ranges.push(cidr);
    } else {
      if (this.newRuleIpv4Open) ipv4Ranges.push('0.0.0.0/0');
      if (this.newRuleIpv6Open) ipv6Ranges.push('::/0');
    }

    const rule: CreateSecurityGroupRuleRequest = {
      protocol: this.newRuleProtocol,
      fromPort: this.newRuleFromPort,
      toPort: this.newRuleToPort,
      ipv4Ranges,
      ipv6Ranges,
      referencedGroupIds: [],
      description: this.newRuleDescription || null
    };

    const request = { rules: [rule] };
    const obs = this.showAddRulePanel === 'inbound'
      ? this.cloudServicesService.addInboundRules(this.sg.id, this.account.id, request)
      : this.cloudServicesService.addOutboundRules(this.sg.id, this.account.id, request);

    obs.subscribe({
      next: (updated) => {
        this.sg = updated;
        this.addRuleSuccess = `${this.showAddRulePanel === 'inbound' ? 'Inbound' : 'Outbound'} rule added successfully.`;
        this.addingRule = false;
        this.resetNewRuleForm();
      },
      error: (err) => {
        this.addRuleError = err?.error?.message || 'Failed to add rule.';
        this.addingRule = false;
      }
    });
  }

  // ══════════════════════════════════════════
  // REMOVE RULES
  // ══════════════════════════════════════════
  removingRules: Record<string, boolean> = {};
  selectedInboundRuleIds: Set<string> = new Set();
  selectedOutboundRuleIds: Set<string> = new Set();
  removingBulk: 'inbound' | 'outbound' | null = null;
  removeRuleError: string | null = null;

  toggleRuleSelection(ruleId: string, type: 'inbound' | 'outbound'): void {
    const set = type === 'inbound' ? this.selectedInboundRuleIds : this.selectedOutboundRuleIds;
    if (set.has(ruleId)) set.delete(ruleId);
    else set.add(ruleId);
  }

  isRuleSelected(ruleId: string, type: 'inbound' | 'outbound'): boolean {
    return type === 'inbound' ? this.selectedInboundRuleIds.has(ruleId) : this.selectedOutboundRuleIds.has(ruleId);
  }

  removeSelectedRules(type: 'inbound' | 'outbound'): void {
    if (!this.sg || !this.account) return;
    const set = type === 'inbound' ? this.selectedInboundRuleIds : this.selectedOutboundRuleIds;
    if (set.size === 0) return;

    this.removingBulk = type;
    this.removeRuleError = null;

    const request = { ruleIds: [...set] };
    const obs = type === 'inbound'
      ? this.cloudServicesService.removeInboundRules(this.sg.id, this.account.id, request)
      : this.cloudServicesService.removeOutboundRules(this.sg.id, this.account.id, request);

    obs.subscribe({
      next: (updated) => {
        this.sg = updated;
        set.clear();
        this.removingBulk = null;
      },
      error: (err) => {
        this.removeRuleError = err?.error?.message || 'Failed to remove rules.';
        this.removingBulk = null;
      }
    });
  }

  removeSingleRule(ruleId: string, type: 'inbound' | 'outbound'): void {
    if (!this.sg || !this.account) return;
    this.removingRules[ruleId] = true;
    this.removeRuleError = null;

    const request = { ruleIds: [ruleId] };
    const obs = type === 'inbound'
      ? this.cloudServicesService.removeInboundRules(this.sg.id, this.account.id, request)
      : this.cloudServicesService.removeOutboundRules(this.sg.id, this.account.id, request);

    obs.subscribe({
      next: (updated) => {
        this.sg = updated;
        delete this.removingRules[ruleId];
        this.selectedInboundRuleIds.delete(ruleId);
        this.selectedOutboundRuleIds.delete(ruleId);
      },
      error: (err) => {
        this.removeRuleError = err?.error?.message || 'Failed to remove rule.';
        delete this.removingRules[ruleId];
      }
    });
  }
}
