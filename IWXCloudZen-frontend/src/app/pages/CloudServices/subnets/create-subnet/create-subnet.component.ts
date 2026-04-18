import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Subnet, CreateSubnetRequest, Vpc } from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-subnet',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-subnet.component.html',
  styleUrls: ['./create-subnet.component.css']
})
export class CreateSubnetComponent implements OnInit {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // VPC selector
  vpcs: Vpc[] = [];
  selectedVpc: Vpc | null = null;
  vpcsLoading = false;
  vpcsError: string | null = null;

  // Form fields
  subnetName = '';
  cidrBlock = '';
  availabilityZone = '';
  mapPublicIpOnLaunch = false;
  ipv6CidrBlock = '';

  formState: FormState = 'loading';
  createdSubnet: Subnet | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;

  // Touched flags
  subnetNameTouched = false;
  cidrBlockTouched = false;
  azTouched = false;

  // Common AZs for quick selection (us-east-1 default, overridden by account region)
  commonAzSuffixes = ['a', 'b', 'c', 'd', 'e', 'f'];

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
    this.availabilityZone = '';
    this.loadVpcs(accountId);
  }

  loadVpcs(accountId: number): void {
    this.vpcsLoading = true;
    this.vpcsError = null;
    this.cloudServicesService.getVpcs(accountId).subscribe({
      next: (res) => {
        this.vpcs = res.vpcs || [];
        this.vpcsLoading = false;
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
    // Suggest AZ based on account region
    if (!this.availabilityZone && this.selectedAccount?.region) {
      this.availabilityZone = `${this.selectedAccount.region}a`;
    }
  }

  getVpcDisplayName(vpc: Vpc): string {
    return vpc.name?.trim() ? vpc.name.trim() : vpc.vpcId;
  }

  navigateToCreateVpc(): void {
    const params: any = { returnTo: '/dashboard/subnets/create' };
    if (this.selectedAccountId) params['accountId'] = this.selectedAccountId;
    this.router.navigate(['/dashboard/vpcs/create'], { queryParams: params });
  }

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  get suggestedAzs(): string[] {
    const region = this.selectedAccount?.region;
    if (!region) return [];
    return this.commonAzSuffixes.map(s => `${region}${s}`);
  }

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      !!this.selectedVpc &&
      this.subnetName.trim().length >= 1 &&
      this.cidrBlock.trim().length >= 1 &&
      !this.cidrBlockError &&
      this.availabilityZone.trim().length >= 1;
  }

  get subnetNameError(): string | null {
    if (!this.subnetNameTouched) return null;
    if (!this.subnetName.trim()) return 'Subnet name is required';
    if (this.subnetName.trim().length > 255) return 'Must be 255 characters or less';
    return null;
  }

  get cidrBlockError(): string | null {
    if (!this.cidrBlockTouched) return null;
    const cidr = this.cidrBlock.trim();
    if (!cidr) return 'CIDR block is required';
    if (!/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\/\d{1,2}$/.test(cidr)) {
      return 'Must be valid CIDR notation e.g. 10.0.1.0/24';
    }
    return null;
  }

  get azError(): string | null {
    if (!this.azTouched) return null;
    if (!this.availabilityZone.trim()) return 'Availability zone is required';
    return null;
  }

  create(): void {
    if (!this.isFormValid || !this.selectedAccountId || !this.selectedVpc) return;

    const request: CreateSubnetRequest = {
      name: this.subnetName.trim(),
      vpcId: this.selectedVpc.vpcId,
      cidrBlock: this.cidrBlock.trim(),
      availabilityZone: this.availabilityZone.trim(),
      mapPublicIpOnLaunch: this.mapPublicIpOnLaunch,
      ipv6CidrBlock: this.ipv6CidrBlock.trim() || null
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

    this.cloudServicesService.createSubnet(this.selectedAccountId, request).subscribe({
      next: (subnet) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdSubnet = subnet;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create subnet. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.subnetName = '';
    this.cidrBlock = '';
    this.availabilityZone = '';
    this.mapPublicIpOnLaunch = false;
    this.ipv6CidrBlock = '';
    this.selectedVpc = null;
    this.subnetNameTouched = false;
    this.cidrBlockTouched = false;
    this.azTouched = false;
    this.createdSubnet = null;
    this.errorMessage = '';
    this.formState = 'form';
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/subnets']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to Subnets';
    const segments = this.returnTo.replace(/^\//, '').split('/').filter(s => s && s !== 'dashboard');
    if (segments.length === 0) return 'Back';
    const label = segments
      .map(s => s.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '))
      .join(' › ');
    return `Back to ${label}`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
