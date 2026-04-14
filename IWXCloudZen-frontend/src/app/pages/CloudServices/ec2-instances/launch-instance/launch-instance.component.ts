import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import {
  Ec2Instance,
  LaunchEc2InstanceRequest,
  KeyPair,
  Subnet,
  SecurityGroup
} from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'launching' | 'success' | 'error';

interface InstanceTypePreset {
  type: string;
  category: string;
  vcpus: string;
  memory: string;
  description: string;
}

@Component({
  selector: 'app-launch-instance',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './launch-instance.component.html',
  styleUrls: ['./launch-instance.component.css']
})
export class LaunchInstanceComponent implements OnInit {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  formState: FormState = 'loading';
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;

  // ── Instance Config ──
  instanceName = '';
  imageId = '';
  selectedInstanceType = '';
  minCount = 1;
  maxCount = 1;
  ebsOptimized = false;
  userData = '';

  // ── Key Pairs ──
  keyPairs: KeyPair[] = [];
  keyPairsLoading = false;
  keyPairsError: string | null = null;
  selectedKeyPair: KeyPair | null = null;

  // ── Subnets ──
  subnets: Subnet[] = [];
  subnetsLoading = false;
  subnetsError: string | null = null;
  selectedSubnet: Subnet | null = null;

  // ── Security Groups (filtered by VPC of selected subnet) ──
  allSecurityGroups: SecurityGroup[] = [];
  securityGroupsLoading = false;
  securityGroupsError: string | null = null;
  selectedSecurityGroupIds: Set<string> = new Set();

  // ── Tags ──
  tags: { key: string; value: string }[] = [];

  // ── Created instance(s) ──
  launchedInstances: Ec2Instance[] = [];

  // ── Touched flags ──
  instanceNameTouched = false;
  imageIdTouched = false;

  // ── AMI presets ──
  amiPresets = [
    { label: 'Amazon Linux 2023', id: 'ami-0c02fb55956c7d316', arch: 'x86_64' },
    { label: 'Ubuntu 22.04 LTS', id: 'ami-0aa7d40eeae50c9a9', arch: 'x86_64' },
    { label: 'Windows Server 2022', id: 'ami-0b5eea76982371e91', arch: 'x86_64' },
    { label: 'Red Hat Enterprise 9', id: 'ami-0fe472d8a85bc7b0e', arch: 'x86_64' },
    { label: 'Debian 12', id: 'ami-058bd2d568351da34', arch: 'x86_64' },
    { label: 'SUSE Linux 15 SP5', id: 'ami-0b36cd6786bcfe120', arch: 'x86_64' }
  ];

  // ── Instance type presets ──
  instanceTypePresets: InstanceTypePreset[] = [
    { type: 't2.micro', category: 'Free Tier', vcpus: '1', memory: '1 GiB', description: 'Burstable, free tier eligible' },
    { type: 't2.small', category: 'General', vcpus: '1', memory: '2 GiB', description: 'Burstable general purpose' },
    { type: 't3.micro', category: 'General', vcpus: '2', memory: '1 GiB', description: 'Next-gen burstable' },
    { type: 't3.small', category: 'General', vcpus: '2', memory: '2 GiB', description: 'Next-gen burstable' },
    { type: 't3.medium', category: 'General', vcpus: '2', memory: '4 GiB', description: 'Next-gen burstable' },
    { type: 't3.large', category: 'General', vcpus: '2', memory: '8 GiB', description: 'Next-gen burstable' },
    { type: 'm5.large', category: 'Compute', vcpus: '2', memory: '8 GiB', description: 'General purpose compute' },
    { type: 'm5.xlarge', category: 'Compute', vcpus: '4', memory: '16 GiB', description: 'General purpose compute' },
    { type: 'c5.large', category: 'Compute', vcpus: '2', memory: '4 GiB', description: 'Compute optimized' },
    { type: 'c5.xlarge', category: 'Compute', vcpus: '4', memory: '8 GiB', description: 'Compute optimized' },
    { type: 'r5.large', category: 'Memory', vcpus: '2', memory: '16 GiB', description: 'Memory optimized' },
    { type: 'r5.xlarge', category: 'Memory', vcpus: '4', memory: '32 GiB', description: 'Memory optimized' }
  ];

  // Instance count presets
  countPresets = [1, 2, 3, 5, 10];

  // User data presets
  userDataPresets = [
    { label: 'None', value: '' },
    { label: 'Update Packages', value: '#!/bin/bash\nyum update -y' },
    { label: 'Install Apache (httpd)', value: '#!/bin/bash\nyum update -y\nyum install -y httpd\nsystemctl start httpd\nsystemctl enable httpd' },
    { label: 'Install Nginx', value: '#!/bin/bash\nyum update -y\namazon-linux-extras install nginx1 -y\nsystemctl start nginx\nsystemctl enable nginx' },
    { label: 'Install Docker', value: '#!/bin/bash\nyum update -y\nyum install -y docker\nsystemctl start docker\nsystemctl enable docker\nusermod -aG docker ec2-user' },
    { label: 'Install Node.js', value: '#!/bin/bash\ncurl -fsSL https://rpm.nodesource.com/setup_20.x | bash -\nyum install -y nodejs' }
  ];

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

        const preSelectId = this.route.snapshot.queryParamMap.get('accountId');
        if (preSelectId) {
          const found = this.accounts.find(a => a.id === +preSelectId);
          if (found) this.selectedAccountId = found.id;
        } else if (this.accounts.length === 1) {
          this.selectedAccountId = this.accounts[0].id;
        }

        this.formState = 'form';
        this.cdr.detectChanges();

        if (this.selectedAccountId) {
          this.onAccountSelected(this.selectedAccountId);
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
    this.resetDependents();
    this.onAccountSelected(accountId);
  }

  private onAccountSelected(accountId: number): void {
    this.loadKeyPairs(accountId);
    this.loadSubnets(accountId);
    this.loadSecurityGroups(accountId);
  }

  private resetDependents(): void {
    this.selectedKeyPair = null;
    this.keyPairs = [];
    this.keyPairsError = null;
    this.selectedSubnet = null;
    this.subnets = [];
    this.subnetsError = null;
    this.allSecurityGroups = [];
    this.selectedSecurityGroupIds.clear();
    this.securityGroupsError = null;
  }

  // ── Key Pairs ──

  loadKeyPairs(accountId: number): void {
    this.keyPairsLoading = true;
    this.keyPairsError = null;
    this.cloudServicesService.getKeyPairs(accountId).subscribe({
      next: (res) => {
        this.keyPairs = res.keyPairs || [];
        this.keyPairsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.keyPairsError = 'Failed to load key pairs.';
        this.keyPairsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectKeyPair(kp: KeyPair): void {
    this.selectedKeyPair = this.selectedKeyPair?.id === kp.id ? null : kp;
  }

  // ── Subnets ──

  loadSubnets(accountId: number): void {
    this.subnetsLoading = true;
    this.subnetsError = null;
    this.cloudServicesService.getSubnets(accountId).subscribe({
      next: (res) => {
        this.subnets = res.subnets || [];
        this.subnetsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.subnetsError = 'Failed to load subnets.';
        this.subnetsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectSubnet(subnet: Subnet): void {
    if (this.selectedSubnet?.id === subnet.id) {
      this.selectedSubnet = null;
      this.selectedSecurityGroupIds.clear();
    } else {
      this.selectedSubnet = subnet;
      // Auto-clear security groups since VPC changed
      this.selectedSecurityGroupIds.clear();
    }
  }

  getSubnetDisplayName(subnet: Subnet): string {
    return subnet.name?.trim() ? subnet.name.trim() : subnet.subnetId;
  }

  navigateToCreateSubnet(): void {
    const params: any = { returnTo: '/dashboard/ec2-instances/create' };
    if (this.selectedAccountId) params['accountId'] = this.selectedAccountId;
    this.router.navigate(['/dashboard/subnets/create'], { queryParams: params });
  }

  // ── Security Groups (VPC-filtered) ──

  loadSecurityGroups(accountId: number): void {
    this.securityGroupsLoading = true;
    this.securityGroupsError = null;
    this.cloudServicesService.getSecurityGroups(accountId).subscribe({
      next: (res) => {
        this.allSecurityGroups = res.securityGroups || [];
        this.securityGroupsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.securityGroupsError = 'Failed to load security groups.';
        this.securityGroupsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get filteredSecurityGroups(): SecurityGroup[] {
    if (!this.selectedSubnet) return [];
    return this.allSecurityGroups.filter(sg => sg.vpcId === this.selectedSubnet!.vpcId);
  }

  toggleSecurityGroup(sg: SecurityGroup): void {
    if (this.selectedSecurityGroupIds.has(sg.securityGroupId)) {
      this.selectedSecurityGroupIds.delete(sg.securityGroupId);
    } else {
      this.selectedSecurityGroupIds.add(sg.securityGroupId);
    }
  }

  isSecurityGroupSelected(sg: SecurityGroup): boolean {
    return this.selectedSecurityGroupIds.has(sg.securityGroupId);
  }

  navigateToCreateSecurityGroup(): void {
    const params: any = { returnTo: '/dashboard/ec2-instances/create' };
    if (this.selectedAccountId) params['accountId'] = this.selectedAccountId;
    this.router.navigate(['/dashboard/security-groups/create'], { queryParams: params });
  }

  // ── Tags ──

  addTag(): void {
    this.tags.push({ key: '', value: '' });
  }

  removeTag(index: number): void {
    this.tags.splice(index, 1);
  }

  // ── Instance Type ──

  selectInstanceType(type: string): void {
    this.selectedInstanceType = this.selectedInstanceType === type ? '' : type;
  }

  get instanceTypeCategories(): string[] {
    return [...new Set(this.instanceTypePresets.map(p => p.category))];
  }

  getPresetsByCategory(category: string): InstanceTypePreset[] {
    return this.instanceTypePresets.filter(p => p.category === category);
  }

  // ── AMI ──

  selectAmi(amiId: string): void {
    this.imageId = this.imageId === amiId ? '' : amiId;
    this.imageIdTouched = true;
  }

  // ── Instance Count ──

  selectCount(count: number): void {
    this.minCount = count;
    this.maxCount = count;
  }

  // ── User Data ──

  selectUserDataPreset(value: string): void {
    this.userData = value;
  }

  // ── Helpers ──

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  get instanceNameError(): string | null {
    if (!this.instanceNameTouched) return null;
    if (!this.instanceName.trim()) return 'Instance name is required';
    if (this.instanceName.trim().length > 255) return 'Must be 255 characters or less';
    return null;
  }

  get imageIdError(): string | null {
    if (!this.imageIdTouched) return null;
    if (!this.imageId.trim()) return 'AMI ID is required';
    if (!/^ami-[a-f0-9]{8,17}$/i.test(this.imageId.trim())) return 'Must be a valid AMI ID (e.g. ami-0c02fb55956c7d316)';
    return null;
  }

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      this.instanceName.trim().length >= 1 &&
      !!this.imageId.trim() && !this.imageIdError &&
      !!this.selectedInstanceType &&
      !!this.selectedSubnet &&
      this.selectedSecurityGroupIds.size > 0 &&
      this.minCount >= 1 && this.maxCount >= this.minCount;
  }

  // ── Launch ──

  launch(): void {
    if (!this.isFormValid || !this.selectedAccountId || !this.selectedSubnet) return;

    const tagsObj: { [key: string]: string } = {};
    this.tags.forEach(t => {
      if (t.key.trim()) tagsObj[t.key.trim()] = t.value.trim();
    });

    const request: LaunchEc2InstanceRequest = {
      instanceName: this.instanceName.trim(),
      imageId: this.imageId.trim(),
      instanceType: this.selectedInstanceType,
      keyName: this.selectedKeyPair?.keyName || '',
      subnetId: this.selectedSubnet.subnetId,
      securityGroupIds: Array.from(this.selectedSecurityGroupIds),
      minCount: this.minCount,
      maxCount: this.maxCount,
      ebsOptimized: this.ebsOptimized,
      userData: this.userData,
      tags: tagsObj
    };

    this.formState = 'launching';
    this.progress = 0;
    this.errorMessage = '';

    this.progressInterval = setInterval(() => {
      if (this.progress < 85) {
        this.progress += Math.random() * 6 + 2;
        this.progress = Math.min(this.progress, 85);
        this.cdr.detectChanges();
      }
    }, 200);

    this.cloudServicesService.launchEc2Instance(this.selectedAccountId, request).subscribe({
      next: (instances) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.launchedInstances = instances;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to launch instance. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.instanceName = '';
    this.imageId = '';
    this.selectedInstanceType = '';
    this.minCount = 1;
    this.maxCount = 1;
    this.ebsOptimized = false;
    this.userData = '';
    this.selectedKeyPair = null;
    this.selectedSubnet = null;
    this.selectedSecurityGroupIds.clear();
    this.tags = [];
    this.instanceNameTouched = false;
    this.imageIdTouched = false;
    this.launchedInstances = [];
    this.errorMessage = '';
    this.formState = 'form';
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/ec2-instances']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to EC2 Instances';
    const segments = this.returnTo.replace(/^\//, '').split('/').filter(s => s && s !== 'dashboard');
    if (segments.length === 0) return 'Back';
    const label = segments
      .map(s => s.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '))
      .join(' › ');
    return `Back to ${label}`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  getSelectedAmiLabel(): string {
    const found = this.amiPresets.find(a => a.id === this.imageId);
    return found ? found.label : 'Custom AMI';
  }
}
