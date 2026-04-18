import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import {
  PermissionSummaryResponse,
  PermissionSummaryPolicy,
  PoliciesResponse,
  PolicyDetail,
  AvailablePolicy,
  SyncPoliciesResponse,
  SyncedPolicy,
  ListPoliciesResponse,
  CheckPermissionResult
} from '../../../models/cloud-services.model';

type ActiveSection = 'summary' | 'policies' | 'browse' | 'check' | 'synced';

interface PermMetric {
  label: string;
  value: string;
  icon: string;
  color: string;
}

// Common AWS actions grouped by service for the permission checker
interface ActionGroup {
  service: string;
  actions: string[];
}

@Component({
  selector: 'app-permissions',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './permissions.component.html',
  styleUrls: ['./permissions.component.css']
})
export class PermissionsComponent implements OnInit, OnDestroy {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;
  loading = true;

  // Section navigation
  activeSection: ActiveSection = 'summary';

  // Summary
  summary: PermissionSummaryResponse | null = null;
  summaryLoading = false;

  // Policies (cloud)
  policiesResponse: PoliciesResponse | null = null;
  policiesLoading = false;
  policiesSearchQuery = '';

  // Browse available policies
  availablePolicies: AvailablePolicy[] = [];
  browseLoading = false;
  browseSearchQuery = '';
  browseScope = 'AWS';
  browseTotalCount = 0;
  attachingArn: string | null = null;

  // Check permissions
  checkResults: CheckPermissionResult[] = [];
  checkLoading = false;
  checkAllowedCount = 0;
  checkDeniedCount = 0;
  selectedActions: string[] = [];
  selectedResource = '*';
  customActionInput = '';

  // Synced policies (DB)
  syncedPolicies: SyncedPolicy[] = [];
  syncedLoading = false;
  syncedSearchQuery = '';
  syncedTotalPolicies = 0;

  // Sync state
  syncing = false;
  lastSyncResult: SyncPoliciesResponse | null = null;
  showSyncReport = false;

  // Toast messages
  toastMessage: string | null = null;
  toastType: 'success' | 'error' | null = null;

  // Detail panel
  selectedPolicy: PolicyDetail | SyncedPolicy | null = null;
  showDetailPanel = false;

  // Detaching state
  detachingArn: string | null = null;

  // Metrics
  metrics: PermMetric[] = [
    { label: 'Managed Policies', value: '—', icon: 'M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z', color: 'text-black' },
    { label: 'Inline Policies', value: '—', icon: 'M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z', color: 'text-blue-600' },
    { label: 'Groups', value: '—', icon: 'M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z', color: 'text-purple-600' },
    { label: 'Cloud Accounts', value: '—', icon: 'M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z', color: 'text-orange-600' }
  ];

  // Sections for navigation
  sections = [
    { key: 'summary' as ActiveSection, label: 'Summary', icon: 'M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z' },
    { key: 'policies' as ActiveSection, label: 'Attached Policies', icon: 'M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z' },
    { key: 'browse' as ActiveSection, label: 'Browse & Attach', icon: 'M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z' },
    { key: 'check' as ActiveSection, label: 'Check Permissions', icon: 'M10.125 2.25h-4.5c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125v-9M10.125 2.25h.375a9 9 0 019 9v.375M10.125 2.25A3.375 3.375 0 0113.5 5.625v1.5c0 .621.504 1.125 1.125 1.125h1.5a3.375 3.375 0 013.375 3.375M9 15l2.25 2.25L15 12' },
    { key: 'synced' as ActiveSection, label: 'Synced Policies', icon: 'M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375m16.5 0v3.75m-16.5-3.75v3.75m16.5 0v3.75C20.25 16.153 16.556 18 12 18s-8.25-1.847-8.25-4.125v-3.75m16.5 0c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125' }
  ];

  // Common AWS action groups for the permission checker
  actionGroups: ActionGroup[] = [
    { service: 'S3', actions: ['s3:GetObject', 's3:PutObject', 's3:DeleteObject', 's3:ListBucket', 's3:CreateBucket', 's3:DeleteBucket', 's3:GetBucketPolicy', 's3:PutBucketPolicy'] },
    { service: 'EC2', actions: ['ec2:DescribeInstances', 'ec2:RunInstances', 'ec2:TerminateInstances', 'ec2:StartInstances', 'ec2:StopInstances', 'ec2:DescribeVpcs', 'ec2:DescribeSubnets', 'ec2:DescribeSecurityGroups', 'ec2:CreateSecurityGroup', 'ec2:AuthorizeSecurityGroupIngress'] },
    { service: 'IAM', actions: ['iam:ListUsers', 'iam:ListRoles', 'iam:ListPolicies', 'iam:CreateRole', 'iam:AttachUserPolicy', 'iam:DetachUserPolicy', 'iam:CreatePolicy', 'iam:DeletePolicy'] },
    { service: 'ECS', actions: ['ecs:ListClusters', 'ecs:DescribeClusters', 'ecs:ListServices', 'ecs:DescribeServices', 'ecs:ListTasks', 'ecs:RunTask', 'ecs:StopTask', 'ecs:CreateService', 'ecs:UpdateService', 'ecs:RegisterTaskDefinition'] },
    { service: 'ECR', actions: ['ecr:GetAuthorizationToken', 'ecr:DescribeRepositories', 'ecr:CreateRepository', 'ecr:DeleteRepository', 'ecr:BatchGetImage', 'ecr:PutImage'] },
    { service: 'CloudWatch', actions: ['logs:CreateLogGroup', 'logs:DeleteLogGroup', 'logs:DescribeLogGroups', 'logs:PutLogEvents', 'logs:GetLogEvents', 'cloudwatch:PutMetricData', 'cloudwatch:GetMetricData'] },
    { service: 'VPC', actions: ['ec2:CreateVpc', 'ec2:DeleteVpc', 'ec2:DescribeVpcs', 'ec2:CreateSubnet', 'ec2:DeleteSubnet', 'ec2:DescribeSubnets', 'ec2:CreateInternetGateway', 'ec2:AttachInternetGateway'] },
    { service: 'Lambda', actions: ['lambda:ListFunctions', 'lambda:CreateFunction', 'lambda:DeleteFunction', 'lambda:InvokeFunction', 'lambda:UpdateFunctionCode', 'lambda:GetFunction'] },
    { service: 'SSM', actions: ['ssm:SendCommand', 'ssm:GetCommandInvocation', 'ssm:StartSession', 'ssm:DescribeInstanceInformation', 'ssm:GetParameter', 'ssm:PutParameter'] },
    { service: 'STS', actions: ['sts:GetCallerIdentity', 'sts:AssumeRole', 'sts:GetSessionToken'] }
  ];

  // Resources for permission checker
  resourceOptions = [
    { value: '*', label: 'All Resources (*)' },
    { value: 'arn:aws:s3:::*', label: 'All S3 Buckets' },
    { value: 'arn:aws:ec2:*:*:instance/*', label: 'All EC2 Instances' },
    { value: 'arn:aws:ecs:*:*:cluster/*', label: 'All ECS Clusters' },
    { value: 'arn:aws:iam::*:role/*', label: 'All IAM Roles' },
    { value: 'arn:aws:iam::*:user/*', label: 'All IAM Users' },
    { value: 'arn:aws:iam::*:policy/*', label: 'All IAM Policies' },
    { value: 'arn:aws:logs:*:*:log-group:*', label: 'All Log Groups' },
    { value: 'arn:aws:ecr:*:*:repository/*', label: 'All ECR Repositories' }
  ];

  // Browse scope options
  scopeOptions = [
    { value: 'AWS', label: 'AWS Managed' },
    { value: 'Local', label: 'Customer Managed' },
    { value: '', label: 'All Scopes' }
  ];

  constructor(
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  // ── Data Loading ──

  private loadAccounts(): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.metrics[3].value = accounts.length.toString();
        if (accounts.length > 0) {
          this.selectedAccountId = accounts[0].id;
          this.loadSummary();
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.metrics[3].value = '0';
      }
    });
  }

  onAccountChange(): void {
    if (!this.selectedAccountId) return;
    // Reset all section data
    this.summary = null;
    this.policiesResponse = null;
    this.availablePolicies = [];
    this.checkResults = [];
    this.syncedPolicies = [];
    this.lastSyncResult = null;
    this.showSyncReport = false;
    this.selectedPolicy = null;
    this.showDetailPanel = false;
    this.loadActiveSection();
  }

  switchSection(section: ActiveSection): void {
    this.activeSection = section;
    this.loadActiveSection();
  }

  private loadActiveSection(): void {
    if (!this.selectedAccountId) return;
    switch (this.activeSection) {
      case 'summary': this.loadSummary(); break;
      case 'policies': this.loadPolicies(); break;
      case 'browse': this.loadAvailablePolicies(); break;
      case 'check': break; // User-initiated
      case 'synced': this.loadSyncedPolicies(); break;
    }
  }

  // ── Summary ──

  loadSummary(): void {
    if (!this.selectedAccountId) return;
    this.summaryLoading = true;
    this.cloudServicesService.getPermissionSummary(this.selectedAccountId).subscribe({
      next: (res) => {
        this.summary = res;
        this.metrics[0].value = res.attachedManagedPoliciesCount.toString();
        this.metrics[1].value = res.inlinePoliciesCount.toString();
        this.metrics[2].value = res.groups.length.toString();
        this.summaryLoading = false;
      },
      error: () => {
        this.summaryLoading = false;
        this.showToast('Failed to load permission summary', 'error');
      }
    });
  }

  // ── Attached Policies ──

  loadPolicies(): void {
    if (!this.selectedAccountId) return;
    this.policiesLoading = true;
    this.cloudServicesService.getPermissionPolicies(this.selectedAccountId).subscribe({
      next: (res) => {
        this.policiesResponse = res;
        this.policiesLoading = false;
      },
      error: () => {
        this.policiesLoading = false;
        this.showToast('Failed to load policies', 'error');
      }
    });
  }

  get filteredPolicies(): PolicyDetail[] {
    if (!this.policiesResponse) return [];
    let result = [...this.policiesResponse.policies];
    if (this.policiesSearchQuery.trim()) {
      const q = this.policiesSearchQuery.toLowerCase();
      result = result.filter(p =>
        p.policyName.toLowerCase().includes(q) ||
        p.policyArn.toLowerCase().includes(q) ||
        p.policyType.toLowerCase().includes(q)
      );
    }
    return result;
  }

  // ── Browse Available Policies ──

  loadAvailablePolicies(): void {
    if (!this.selectedAccountId) return;
    this.browseLoading = true;
    this.cloudServicesService.browseAvailablePolicies(
      this.selectedAccountId, this.browseScope, this.browseSearchQuery
    ).subscribe({
      next: (res) => {
        this.availablePolicies = res.policies;
        this.browseTotalCount = res.totalCount;
        this.browseLoading = false;
      },
      error: () => {
        this.browseLoading = false;
        this.showToast('Failed to browse policies', 'error');
      }
    });
  }

  onBrowseSearch(): void {
    this.loadAvailablePolicies();
  }

  onBrowseScopeChange(): void {
    this.loadAvailablePolicies();
  }

  attachPolicy(policy: AvailablePolicy): void {
    if (!this.selectedAccountId) return;
    this.attachingArn = policy.policyArn;
    this.cloudServicesService.attachPolicy(this.selectedAccountId, { policyArn: policy.policyArn }).subscribe({
      next: (res) => {
        this.showToast(res.message, 'success');
        this.attachingArn = null;
        // Refresh policies
        this.loadPolicies();
        this.loadSummary();
      },
      error: (err) => {
        this.showToast(err.error?.message || 'Failed to attach policy', 'error');
        this.attachingArn = null;
      }
    });
  }

  isAlreadyAttached(policyArn: string): boolean {
    if (!this.policiesResponse) return false;
    return this.policiesResponse.policies.some(p => p.policyArn === policyArn);
  }

  // ── Detach Policy ──

  detachPolicy(policyArn: string): void {
    if (!this.selectedAccountId) return;
    this.detachingArn = policyArn;
    this.cloudServicesService.detachPolicy(this.selectedAccountId, policyArn).subscribe({
      next: (res) => {
        this.showToast(res.message, 'success');
        this.detachingArn = null;
        this.loadPolicies();
        this.loadSummary();
      },
      error: (err) => {
        this.showToast(err.error?.message || 'Failed to detach policy', 'error');
        this.detachingArn = null;
      }
    });
  }

  // ── Check Permissions ──

  toggleAction(action: string): void {
    const idx = this.selectedActions.indexOf(action);
    if (idx >= 0) {
      this.selectedActions.splice(idx, 1);
    } else {
      this.selectedActions.push(action);
    }
  }

  isActionSelected(action: string): boolean {
    return this.selectedActions.includes(action);
  }

  selectAllServiceActions(group: ActionGroup): void {
    const allSelected = group.actions.every(a => this.selectedActions.includes(a));
    if (allSelected) {
      // Deselect all
      this.selectedActions = this.selectedActions.filter(a => !group.actions.includes(a));
    } else {
      // Select all missing
      for (const a of group.actions) {
        if (!this.selectedActions.includes(a)) {
          this.selectedActions.push(a);
        }
      }
    }
  }

  isAllServiceSelected(group: ActionGroup): boolean {
    return group.actions.every(a => this.selectedActions.includes(a));
  }

  isSomeServiceSelected(group: ActionGroup): boolean {
    return group.actions.some(a => this.selectedActions.includes(a)) && !this.isAllServiceSelected(group);
  }

  clearSelectedActions(): void {
    this.selectedActions = [];
    this.checkResults = [];
    this.checkAllowedCount = 0;
    this.checkDeniedCount = 0;
  }

  runPermissionCheck(): void {
    if (!this.selectedAccountId || this.selectedActions.length === 0) return;
    this.checkLoading = true;
    this.cloudServicesService.checkPermissions(this.selectedAccountId, {
      actions: this.selectedActions,
      resourceArns: [this.selectedResource]
    }).subscribe({
      next: (res) => {
        this.checkResults = res.results;
        this.checkAllowedCount = res.allowedCount;
        this.checkDeniedCount = res.deniedCount;
        this.checkLoading = false;
      },
      error: () => {
        this.checkLoading = false;
        this.showToast('Failed to check permissions', 'error');
      }
    });
  }

  // ── Synced Policies (DB) ──

  loadSyncedPolicies(): void {
    if (!this.selectedAccountId) return;
    this.syncedLoading = true;
    this.cloudServicesService.listPermissionPolicies(this.selectedAccountId).subscribe({
      next: (res) => {
        this.syncedPolicies = res.policies;
        this.syncedTotalPolicies = res.totalPolicies;
        this.syncedLoading = false;
      },
      error: () => {
        this.syncedLoading = false;
        this.showToast('Failed to load synced policies', 'error');
      }
    });
  }

  get filteredSyncedPolicies(): SyncedPolicy[] {
    let result = [...this.syncedPolicies];
    if (this.syncedSearchQuery.trim()) {
      const q = this.syncedSearchQuery.toLowerCase();
      result = result.filter(p =>
        p.policyName.toLowerCase().includes(q) ||
        p.policyArn.toLowerCase().includes(q) ||
        p.policyType.toLowerCase().includes(q)
      );
    }
    return result;
  }

  // ── Sync from Cloud ──

  syncPolicies(): void {
    if (!this.selectedAccountId) return;
    this.syncing = true;
    this.cloudServicesService.syncPermissionPolicies(this.selectedAccountId).subscribe({
      next: (res) => {
        this.lastSyncResult = res;
        this.showSyncReport = true;
        this.syncing = false;
        this.showToast(`Sync complete: ${res.added} added, ${res.updated} updated, ${res.removed} removed`, 'success');
        // Reload synced policies and summary
        this.loadSyncedPolicies();
        this.loadSummary();
      },
      error: () => {
        this.syncing = false;
        this.showToast('Failed to sync policies', 'error');
      }
    });
  }

  closeSyncReport(): void {
    this.showSyncReport = false;
  }

  // ── Detail Panel ──

  openPolicyDetail(policy: PolicyDetail | SyncedPolicy): void {
    this.selectedPolicy = policy;
    this.showDetailPanel = true;
    document.body.style.overflow = 'hidden';
  }

  closeDetail(): void {
    this.showDetailPanel = false;
    setTimeout(() => {
      this.selectedPolicy = null;
      document.body.style.overflow = '';
    }, 300);
  }

  // ── Toast ──

  showToast(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    setTimeout(() => {
      this.toastMessage = null;
      this.toastType = null;
    }, 5000);
  }

  // ── Utility ──

  getAccountName(accountId: number): string {
    const acc = this.accounts.find(a => a.id === accountId);
    return acc ? acc.accountName : `Account #${accountId}`;
  }

  getPolicyTypeClass(type: string): string {
    const t = type?.toLowerCase();
    if (t === 'aws managed') return 'bg-blue-50 text-blue-700 border-blue-200';
    if (t === 'inline') return 'bg-amber-50 text-amber-700 border-amber-200';
    if (t === 'customer managed' || t === 'local') return 'bg-purple-50 text-purple-700 border-purple-200';
    return 'bg-gray-50 text-gray-700 border-gray-200';
  }

  getDecisionClass(decision: string): string {
    if (decision === 'allowed') return 'bg-green-50 text-green-700 border-green-200';
    return 'bg-red-50 text-red-700 border-red-200';
  }

  getDecisionDotClass(isAllowed: boolean): string {
    return isAllowed ? 'bg-green-500' : 'bg-red-500';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(dateStr));
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    return new Intl.DateTimeFormat('en-US', {
      month: 'short', day: 'numeric', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    }).format(new Date(dateStr));
  }

  refreshData(): void {
    this.loadActiveSection();
    this.loadSummary();
  }

  truncateArn(arn: string): string {
    if (!arn || arn.length <= 50) return arn;
    return arn.substring(0, 25) + '...' + arn.substring(arn.length - 22);
  }

  // Expandable action groups in Check section
  expandedGroups: Set<string> = new Set();

  toggleGroupExpand(service: string): void {
    if (this.expandedGroups.has(service)) {
      this.expandedGroups.delete(service);
    } else {
      this.expandedGroups.add(service);
    }
  }

  isGroupExpanded(service: string): boolean {
    return this.expandedGroups.has(service);
  }

  getSelectedCountForGroup(group: ActionGroup): number {
    return group.actions.filter(a => this.selectedActions.includes(a)).length;
  }
}
