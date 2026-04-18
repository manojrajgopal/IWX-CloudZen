import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { KeyPair } from '../../../models/cloud-services.model';

@Component({
  selector: 'app-sensitive-data-viewer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './sensitive-data-viewer.component.html',
  styleUrls: ['./sensitive-data-viewer.component.css']
})
export class SensitiveDataViewerComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;

  keyPair: KeyPair | null = null;
  privateKeyMaterial: string | null = null;

  // Timer
  totalSeconds = 60;
  remainingSeconds = 60;
  private timerInterval: any;
  timerExpired = false;

  // State
  downloaded = false;
  copiedKey = false;
  private copiedTimeout: any;

  // Return navigation
  private returnTo: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {
    this.returnTo = this.route.snapshot.queryParamMap.get('returnTo') || '/dashboard/key-pairs';
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.error = 'Invalid key pair ID';
      this.loading = false;
      return;
    }
    this.loadData(id);
  }

  ngOnDestroy(): void {
    if (this.timerInterval) clearInterval(this.timerInterval);
    if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
  }

  private loadData(keyPairId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => this.findKeyPairAndDownload(accounts, keyPairId),
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.loading = false;
      }
    });
  }

  private findKeyPairAndDownload(accounts: CloudAccount[], keyPairId: number): void {
    if (accounts.length === 0) {
      this.error = 'No cloud accounts found';
      this.loading = false;
      return;
    }
    const requests = accounts.map(a =>
      this.cloudServicesService.getKeyPairs(a.id).pipe(catchError(() => of({ keyPairs: [] })))
    );
    forkJoin(requests).subscribe({
      next: (results: any[]) => {
        const all: KeyPair[] = results.flatMap((r: any) => r.keyPairs || []);
        this.keyPair = all.find(k => k.id === keyPairId) || null;
        if (!this.keyPair) {
          this.error = 'Key pair not found';
          this.loading = false;
          return;
        }
        if (!this.keyPair.hasPrivateKey) {
          this.error = 'No private key available for this key pair';
          this.loading = false;
          return;
        }
        this.fetchPrivateKey(this.keyPair);
      },
      error: () => {
        this.error = 'Failed to load key pair data';
        this.loading = false;
      }
    });
  }

  private fetchPrivateKey(keyPair: KeyPair): void {
    this.cloudServicesService.downloadPrivateKey(keyPair.id, keyPair.cloudAccountId).subscribe({
      next: (response) => {
        this.privateKeyMaterial = response.privateKeyMaterial;
        this.loading = false;
        this.startTimer();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to retrieve private key';
        this.loading = false;
      }
    });
  }

  private startTimer(): void {
    this.remainingSeconds = this.totalSeconds;
    this.timerInterval = setInterval(() => {
      this.remainingSeconds--;
      if (this.remainingSeconds <= 0) {
        clearInterval(this.timerInterval);
        this.timerExpired = true;
        this.privateKeyMaterial = null;
        setTimeout(() => this.navigateBack(), 1500);
      }
    }, 1000);
  }

  get timerPercent(): number {
    return (this.remainingSeconds / this.totalSeconds) * 100;
  }

  get timerColor(): string {
    if (this.remainingSeconds > 30) return '#22c55e';
    if (this.remainingSeconds > 10) return '#eab308';
    return '#ef4444';
  }

  get formattedTime(): string {
    const mins = Math.floor(this.remainingSeconds / 60);
    const secs = this.remainingSeconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  downloadPem(): void {
    if (!this.privateKeyMaterial || !this.keyPair) return;
    const blob = new Blob([this.privateKeyMaterial], { type: 'application/x-pem-file' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${this.keyPair.keyName}.pem`;
    a.click();
    URL.revokeObjectURL(url);
    this.downloaded = true;
  }

  copyToClipboard(): void {
    if (!this.privateKeyMaterial) return;
    navigator.clipboard.writeText(this.privateKeyMaterial).then(() => {
      this.copiedKey = true;
      if (this.copiedTimeout) clearTimeout(this.copiedTimeout);
      this.copiedTimeout = setTimeout(() => this.copiedKey = false, 3000);
    });
  }

  navigateBack(): void {
    this.router.navigate([this.returnTo]);
  }
}
