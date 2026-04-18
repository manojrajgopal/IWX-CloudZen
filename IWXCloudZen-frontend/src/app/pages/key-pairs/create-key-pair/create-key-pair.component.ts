import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { CloudAccountService } from '../../../services/cloud-account.service';
import { CloudServicesService } from '../../../services/cloud-services.service';
import { CloudAccount } from '../../../models/cloud-account.model';
import { KeyPair, CreateKeyPairRequest } from '../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-key-pair',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-key-pair.component.html',
  styleUrls: ['./create-key-pair.component.css']
})
export class CreateKeyPairComponent implements OnInit, OnDestroy {
  formState: FormState = 'loading';
  accounts: CloudAccount[] = [];

  // Form fields
  selectedAccountId: number | null = null;
  keyName = '';
  selectedKeyType = 'rsa';
  tagEntries: { key: string; value: string }[] = [];

  // Validation
  keyNameTouched = false;

  // Result
  createdKeyPair: KeyPair | null = null;
  errorMessage: string | null = null;

  // Progress
  progressPercent = 0;
  private progressInterval: any;

  keyTypeOptions = [
    { value: 'rsa', label: 'RSA', desc: 'RSA 2048-bit key — widely compatible' },
    { value: 'ed25519', label: 'ED25519', desc: 'Ed25519 key — modern, faster, smaller' }
  ];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
  }

  ngOnDestroy(): void {
    if (this.progressInterval) clearInterval(this.progressInterval);
  }

  private loadAccounts(): void {
    this.formState = 'loading';
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        if (accounts.length === 1) this.selectedAccountId = accounts[0].id;
        this.formState = 'form';
      },
      error: () => {
        this.errorMessage = 'Failed to load cloud accounts.';
        this.formState = 'error';
      }
    });
  }

  selectAccount(id: number): void {
    this.selectedAccountId = id;
  }

  selectKeyType(type: string): void {
    this.selectedKeyType = type;
  }

  addTag(): void {
    this.tagEntries.push({ key: '', value: '' });
  }

  removeTag(index: number): void {
    this.tagEntries.splice(index, 1);
  }

  get isFormValid(): boolean {
    return this.selectedAccountId !== null && this.keyName.trim().length > 0;
  }

  createKeyPair(): void {
    if (!this.isFormValid || this.formState === 'creating') return;

    this.formState = 'creating';
    this.progressPercent = 0;
    this.errorMessage = null;

    this.progressInterval = setInterval(() => {
      if (this.progressPercent < 90) {
        this.progressPercent += Math.random() * 15;
        if (this.progressPercent > 90) this.progressPercent = 90;
      }
    }, 200);

    const tags: { [key: string]: string } = {};
    for (const entry of this.tagEntries) {
      if (entry.key.trim()) tags[entry.key.trim()] = entry.value.trim();
    }

    const request: CreateKeyPairRequest = {
      keyName: this.keyName.trim(),
      keyType: this.selectedKeyType,
    };
    if (Object.keys(tags).length > 0) request.tags = tags;

    this.cloudServicesService.createKeyPair(this.selectedAccountId!, request).subscribe({
      next: (result) => {
        clearInterval(this.progressInterval);
        this.progressPercent = 100;
        this.createdKeyPair = result;
        setTimeout(() => this.formState = 'success', 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progressPercent = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create key pair.';
        this.formState = 'error';
      }
    });
  }

  viewSensitiveData(): void {
    if (!this.createdKeyPair) return;
    this.router.navigate(['/dashboard/key-pairs', this.createdKeyPair.id, 'private-key'], {
      queryParams: { returnTo: '/dashboard/key-pairs' }
    });
  }

  goToOverview(): void {
    if (!this.createdKeyPair) return;
    this.router.navigate(['/dashboard/key-pairs', this.createdKeyPair.id]);
  }

  goBack(): void {
    const returnTo = this.route.snapshot.queryParamMap.get('returnTo');
    this.router.navigate([returnTo || '/dashboard/key-pairs']);
  }

  retryCreate(): void {
    this.formState = 'form';
    this.errorMessage = null;
  }

  getSelectedAccountName(): string {
    if (!this.selectedAccountId) return '';
    return this.accounts.find(a => a.id === this.selectedAccountId)?.accountName || '';
  }
}
