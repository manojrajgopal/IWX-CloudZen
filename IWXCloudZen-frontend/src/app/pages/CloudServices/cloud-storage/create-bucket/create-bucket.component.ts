import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { S3Bucket } from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-bucket',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-bucket.component.html',
  styleUrls: ['./create-bucket.component.css']
})
export class CreateBucketComponent implements OnInit, AfterViewInit {
  @ViewChild('cloudPath') cloudPath!: ElementRef<SVGPathElement>;
  @ViewChild('bucketInput') bucketInput!: ElementRef<HTMLInputElement>;

  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;
  bucketName = '';
  formState: FormState = 'loading';
  createdBucket: S3Bucket | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  // Validation
  bucketNameTouched = false;

  constructor(
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
  }

  ngAfterViewInit(): void {
    this.animateCloudPath();
  }

  private animateCloudPath(): void {
    try {
      const path = this.cloudPath?.nativeElement;
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
        setTimeout(() => this.bucketInput?.nativeElement?.focus(), 200);
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
    return !!this.selectedAccountId && this.bucketName.trim().length >= 3;
  }

  get bucketNameError(): string | null {
    if (!this.bucketNameTouched) return null;
    const name = this.bucketName.trim();
    if (name.length === 0) return 'Bucket name is required';
    if (name.length < 3) return 'Bucket name must be at least 3 characters';
    if (name.length > 63) return 'Bucket name must be 63 characters or less';
    if (!/^[a-z0-9][a-z0-9.-]*[a-z0-9]$/.test(name) && name.length > 1) {
      return 'Only lowercase letters, numbers, hyphens, and dots allowed';
    }
    if (/\.\./.test(name)) return 'Consecutive dots are not allowed';
    if (/^(\d{1,3}\.){3}\d{1,3}$/.test(name)) return 'Bucket name cannot resemble an IP address';
    return null;
  }

  createBucket(): void {
    if (!this.isFormValid || !this.selectedAccountId) return;

    this.formState = 'creating';
    this.progress = 0;
    this.errorMessage = '';

    // Animate progress
    this.progressInterval = setInterval(() => {
      if (this.progress < 85) {
        this.progress += Math.random() * 8 + 2;
        this.progress = Math.min(this.progress, 85);
        this.cdr.detectChanges();
      }
    }, 150);

    this.cloudServicesService.createS3Bucket(this.selectedAccountId, {
      bucketName: this.bucketName.trim()
    }).subscribe({
      next: (bucket) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdBucket = bucket;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create bucket. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.bucketName = '';
    this.bucketNameTouched = false;
    this.createdBucket = null;
    this.errorMessage = '';
    this.formState = 'form';
    setTimeout(() => this.bucketInput?.nativeElement?.focus(), 200);
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard/cloud-storage']);
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
