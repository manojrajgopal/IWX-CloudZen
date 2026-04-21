import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { InternetGateway, Vpc } from '../../../../models/cloud-services.model';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-internet-gateway',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-internet-gateway.component.html',
  styleUrls: ['./create-internet-gateway.component.css']
})
export class CreateInternetGatewayComponent implements OnInit, AfterViewInit {
  @ViewChild('igwPath') igwPath!: ElementRef<SVGPathElement>;
  @ViewChild('igwNameInput') igwNameInput!: ElementRef<HTMLInputElement>;

  accounts: CloudAccount[] = [];
  vpcs: Vpc[] = [];
  selectedAccountId: number | null = null;
  selectedVpcId: string | null = null;
  igwName = '';
  formState: FormState = 'loading';
  createdIgw: InternetGateway | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;
  returnTo: string | null = null;

  // Validation
  igwNameTouched = false;

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
    this.animateIgwPath();
  }

  private animateIgwPath(): void {
    try {
      const path = this.igwPath?.nativeElement;
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
          this.loadVpcsForAccount(this.accounts[0].id);
        }
        this.formState = 'form';
        this.cdr.detectChanges();
        setTimeout(() => this.igwNameInput?.nativeElement?.focus(), 200);
      },
      error: () => {
        this.errorMessage = 'Failed to load cloud accounts. Please try again.';
        this.formState = 'error';
      }
    });
  }

  private loadVpcsForAccount(accountId: number): void {
    this.cloudServicesService.getVpcs(accountId).pipe(
      catchError(() => of({ vpcs: [] }))
    ).subscribe((res: any) => {
      this.vpcs = res.vpcs || [];
      this.cdr.detectChanges();
    });
  }

  selectAccount(accountId: number): void {
    this.selectedAccountId = accountId;
    this.selectedVpcId = null;
    this.vpcs = [];
    this.loadVpcsForAccount(accountId);
  }

  selectVpc(vpcId: string): void {
    this.selectedVpcId = this.selectedVpcId === vpcId ? null : vpcId;
  }

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  get selectedVpc(): Vpc | null {
    return this.vpcs.find(v => v.vpcId === this.selectedVpcId) || null;
  }

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      this.igwName.trim().length >= 1 &&
      !this.igwNameError &&
      !!this.selectedVpcId;
  }

  get igwNameError(): string | null {
    if (!this.igwNameTouched) return null;
    const name = this.igwName.trim();
    if (name.length === 0) return 'Internet Gateway name is required';
    if (name.length > 255) return 'Name must be 255 characters or less';
    if (!/^[a-zA-Z][a-zA-Z0-9\-_]*$/.test(name)) {
      return 'Must start with a letter; only letters, numbers, hyphens, and underscores allowed';
    }
    return null;
  }

  createInternetGateway(): void {
    if (!this.isFormValid || !this.selectedAccountId || !this.selectedVpcId) return;

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

    this.cloudServicesService.createInternetGateway(this.selectedAccountId, {
      name: this.igwName.trim(),
      vpcId: this.selectedVpcId
    }).subscribe({
      next: (igw) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdIgw = igw;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create Internet Gateway. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.igwName = '';
    this.selectedVpcId = null;
    this.igwNameTouched = false;
    this.createdIgw = null;
    this.errorMessage = '';
    this.formState = 'form';
    setTimeout(() => this.igwNameInput?.nativeElement?.focus(), 200);
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/internet-gateways']);
    }
  }

  goToDashboard(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/internet-gateways']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to Internet Gateways';
    const segments = this.returnTo.replace(/^\//, '').split('/').filter(s => s && s !== 'dashboard');
    if (segments.length === 0) return 'Back';
    const label = segments
      .map(s => s.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '))
      .join(' › ');
    return `Back to ${label}`;
  }

  getVpcDisplayName(vpc: Vpc): string {
    return vpc.name || vpc.vpcId;
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
