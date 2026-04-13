import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Vpc } from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-vpc',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-vpc.component.html',
  styleUrls: ['./create-vpc.component.css']
})
export class CreateVpcComponent implements OnInit, AfterViewInit {
  @ViewChild('vpcPath') vpcPath!: ElementRef<SVGPathElement>;
  @ViewChild('vpcNameInput') vpcNameInput!: ElementRef<HTMLInputElement>;

  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;
  vpcName = '';
  cidrBlock = '10.0.0.0/16';
  enableDnsSupport = true;
  enableDnsHostnames = true;
  formState: FormState = 'loading';
  createdVpc: Vpc | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;
  returnTo: string | null = null;

  // Validation
  vpcNameTouched = false;
  cidrBlockTouched = false;

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

  ngAfterViewInit(): void {
    this.animateVpcPath();
  }

  private animateVpcPath(): void {
    try {
      const path = this.vpcPath?.nativeElement;
      if (!path) return;
      const length = path.getTotalLength();
      if (length > 0) {
        path.style.strokeDasharray = `${length}`;
        path.style.strokeDashoffset = `${length}`;
        path.getBoundingClientRect();
        path.style.animation = 'draw 2s ease-in-out forwards';
      }
    } catch {}
  }

  private loadAccounts(): void {
    this.formState = 'loading';
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts.filter(a => a.provider?.toUpperCase() === 'AWS');
        if (this.accounts.length === 1) {
          this.selectedAccountId = this.accounts[0].id;
        }
        this.formState = 'form';
        this.cdr.detectChanges();
        setTimeout(() => this.vpcNameInput?.nativeElement?.focus(), 200);
      },
      error: () => {
        this.errorMessage = 'Failed to load cloud accounts. Please try again.';
        this.formState = 'error';
      }
    });
  }

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      this.vpcName.trim().length >= 1 &&
      !this.vpcNameError &&
      !this.cidrBlockError;
  }

  get vpcNameError(): string | null {
    if (!this.vpcNameTouched) return null;
    const name = this.vpcName.trim();
    if (name.length === 0) return 'VPC name is required';
    if (name.length > 255) return 'VPC name must be 255 characters or less';
    if (!/^[a-zA-Z][a-zA-Z0-9\-_]*$/.test(name)) {
      return 'Must start with a letter; only letters, numbers, hyphens, and underscores allowed';
    }
    return null;
  }

  get cidrBlockError(): string | null {
    if (!this.cidrBlockTouched) return null;
    const cidr = this.cidrBlock.trim();
    if (cidr.length === 0) return 'CIDR block is required';
    if (!/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\/\d{1,2}$/.test(cidr)) {
      return 'Must be a valid CIDR notation (e.g. 10.0.0.0/16)';
    }
    return null;
  }

  createVpc(): void {
    if (!this.isFormValid || !this.selectedAccountId) return;

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

    this.cloudServicesService.createVpc(this.selectedAccountId, {
      vpcName: this.vpcName.trim(),
      cidrBlock: this.cidrBlock.trim(),
      enableDnsSupport: this.enableDnsSupport,
      enableDnsHostnames: this.enableDnsHostnames
    }).subscribe({
      next: (vpc) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdVpc = vpc;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create VPC. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.vpcName = '';
    this.cidrBlock = '10.0.0.0/16';
    this.enableDnsSupport = true;
    this.enableDnsHostnames = true;
    this.vpcNameTouched = false;
    this.cidrBlockTouched = false;
    this.createdVpc = null;
    this.errorMessage = '';
    this.formState = 'form';
    setTimeout(() => this.vpcNameInput?.nativeElement?.focus(), 200);
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/vpcs']);
    }
  }

  goToDashboard(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/vpcs']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to VPCs';
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
    return d.toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
