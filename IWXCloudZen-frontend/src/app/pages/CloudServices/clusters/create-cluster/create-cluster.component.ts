import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Cluster } from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-cluster',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-cluster.component.html',
  styleUrls: ['./create-cluster.component.css']
})
export class CreateClusterComponent implements OnInit, AfterViewInit {
  @ViewChild('clusterPath') clusterPath!: ElementRef<SVGPathElement>;
  @ViewChild('clusterInput') clusterInput!: ElementRef<HTMLInputElement>;

  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;
  clusterName = '';
  formState: FormState = 'loading';
  createdCluster: Cluster | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;

  // Validation
  clusterNameTouched = false;

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
    this.animateClusterPath();
  }

  private animateClusterPath(): void {
    try {
      const path = this.clusterPath?.nativeElement;
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
        const preSelectId = this.route.snapshot.queryParamMap.get('accountId');
        if (preSelectId) {
          const found = this.accounts.find(a => a.id === +preSelectId);
          if (found) this.selectedAccountId = found.id;
        } else if (this.accounts.length === 1) {
          this.selectedAccountId = this.accounts[0].id;
        }
        this.formState = 'form';
        this.cdr.detectChanges();
        setTimeout(() => this.clusterInput?.nativeElement?.focus(), 200);
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
    return !!this.selectedAccountId && this.clusterName.trim().length >= 1;
  }

  get clusterNameError(): string | null {
    if (!this.clusterNameTouched) return null;
    const name = this.clusterName.trim();
    if (name.length === 0) return 'Cluster name is required';
    if (name.length > 255) return 'Cluster name must be 255 characters or less';
    if (!/^[a-zA-Z][a-zA-Z0-9\-_]*$/.test(name)) {
      return 'Must start with a letter; only letters, numbers, hyphens, and underscores allowed';
    }
    return null;
  }

  createCluster(): void {
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

    this.cloudServicesService.createCluster(this.selectedAccountId, {
      clusterName: this.clusterName.trim()
    }).subscribe({
      next: (cluster) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdCluster = cluster;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create cluster. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.clusterName = '';
    this.clusterNameTouched = false;
    this.createdCluster = null;
    this.errorMessage = '';
    this.formState = 'form';
    setTimeout(() => this.clusterInput?.nativeElement?.focus(), 200);
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/clusters']);
    }
  }

  goToDashboard(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/clusters']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to Clusters';
    const segments = this.returnTo.replace(/^\//, '').split('/').filter(s => s && s !== 'dashboard');
    if (segments.length === 0) return 'Back';
    const label = segments
      .map(s => s.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '))
      .join(' › ');
    return `Back to ${label}`;
  }

  goToCluster(): void {
    if (this.createdCluster) {
      this.router.navigate(['/dashboard/clusters', this.createdCluster.id]);
    }
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
