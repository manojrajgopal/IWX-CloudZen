import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { CloudAccountService, ConnectAccountRequest } from '../../services/cloud-account.service';
import { CloudAccount, CloudProvider } from '../../models/cloud-account.model';

interface ProfileStat {
  label: string;
  value: string;
}

interface ActivityItem {
  icon: string;
  title: string;
  time: string;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  currentUser: any = null;
  cloudAccounts: CloudAccount[] = [];
  providers: CloudProvider[] = [];
  loadingAccounts = true;
  accountsError = '';

  // Modal states
  showCreateModal = false;
  showManageModal = false;
  showSettingsModal = false;
  showDeleteModal = false;
  modalClosing = false;
  selectedAccount: CloudAccount | null = null;
  settingDefault = false;

  // Settings toggles (persisted in localStorage)
  settings: { [key: string]: boolean } = {
    autoValidation: true,
    costAlerts: false,
    resourceMonitoring: true,
    securityScanning: false,
    emailNotifications: true,
    smsAlerts: false,
    inAppNotifications: true
  };

  // Create form
  createLoading = false;
  createError = '';
  createSuccess = '';
  connectForm: ConnectAccountRequest = {
    Provider: '',
    AccountName: '',
    AccessKey: '',
    SecretKey: '',
    TenantId: null,
    ClientId: null,
    ClientSecret: null,
    Region: '',
    MakeDefault: false
  };

  profileStats: ProfileStat[] = [
    { label: 'Cloud Accounts', value: '—' },
    { label: 'Active Services', value: '12' },
    { label: 'Deployments', value: '87' },
    { label: 'Uptime', value: '99.9%' }
  ];

  recentActivity: ActivityItem[] = [
    { icon: 'EC2', title: 'Launched 2 new EC2 instances in ap-south-1', time: '30 minutes ago' },
    { icon: 'S3', title: 'Created S3 bucket: prod-assets-042026', time: '2 hours ago' },
    { icon: 'ECS', title: 'Deployed v2.4.1 to ECS cluster', time: '5 hours ago' },
    { icon: 'IAM', title: 'Updated IAM policy for dev-team role', time: 'Yesterday' },
    { icon: 'VPC', title: 'Created new VPC peering connection', time: '2 days ago' }
  ];

  constructor(
    private authService: AuthService,
    private cloudAccountService: CloudAccountService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getUser();
    this.loadCloudAccounts();
    this.loadProviders();
    this.loadSettings();
  }

  loadCloudAccounts(): void {
    this.loadingAccounts = true;
    this.accountsError = '';
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.cloudAccounts = accounts.sort((a, b) => (b.isDefault ? 1 : 0) - (a.isDefault ? 1 : 0));
        this.profileStats[0].value = accounts.length.toString();
        this.loadingAccounts = false;
      },
      error: () => {
        this.accountsError = 'Failed to load cloud accounts.';
        this.loadingAccounts = false;
      }
    });
  }

  loadProviders(): void {
    this.cloudAccountService.getProviders().subscribe({
      next: (providers) => {
        this.providers = providers;
      }
    });
  }

  // --- Create Modal ---
  openCreateModal(): void {
    this.resetConnectForm();
    this.createError = '';
    this.createSuccess = '';
    this.showCreateModal = true;
    this.modalClosing = false;
    document.body.style.overflow = 'hidden';
  }

  closeCreateModal(): void {
    this.modalClosing = true;
    setTimeout(() => {
      this.showCreateModal = false;
      this.modalClosing = false;
      document.body.style.overflow = '';
    }, 300);
  }

  resetConnectForm(): void {
    this.connectForm = {
      Provider: '',
      AccountName: '',
      AccessKey: '',
      SecretKey: '',
      TenantId: null,
      ClientId: null,
      ClientSecret: null,
      Region: '',
      MakeDefault: false
    };
  }

  getSelectedProviderFields(): string[] {
    const provider = this.providers.find(p => p.value === this.connectForm.Provider);
    return provider ? provider.requiredFields : [];
  }

  isFieldRequired(field: string): boolean {
    return this.getSelectedProviderFields().includes(field);
  }

  submitConnect(): void {
    this.createLoading = true;
    this.createError = '';
    this.createSuccess = '';
    this.cloudAccountService.connectAccount(this.connectForm).subscribe({
      next: (response) => {
        this.createSuccess = response.message;
        this.createLoading = false;
        this.loadCloudAccounts();
        setTimeout(() => this.closeCreateModal(), 1500);
      },
      error: (err) => {
        this.createError = err.error?.message || 'Failed to connect account.';
        this.createLoading = false;
      }
    });
  }

  // --- Manage Modal ---
  openManageModal(account: CloudAccount): void {
    this.selectedAccount = account;
    this.showManageModal = true;
    this.modalClosing = false;
    document.body.style.overflow = 'hidden';
  }

  closeManageModal(): void {
    this.modalClosing = true;
    setTimeout(() => {
      this.showManageModal = false;
      this.modalClosing = false;
      this.selectedAccount = null;
      document.body.style.overflow = '';
    }, 300);
  }

  setDefaultAccount(): void {
    if (!this.selectedAccount || this.selectedAccount.isDefault || this.cloudAccounts.length <= 1 || this.settingDefault) return;
    this.settingDefault = true;
    this.cloudAccountService.setDefaultAccount(this.selectedAccount.id).subscribe({
      next: (response) => {
        this.cloudAccounts.forEach(a => a.isDefault = a.id === this.selectedAccount!.id);
        this.selectedAccount!.isDefault = true;
        this.settingDefault = false;
      },
      error: () => {
        this.settingDefault = false;
      }
    });
  }

  // --- Settings Modal ---
  openSettingsModal(account: CloudAccount): void {
    this.selectedAccount = account;
    this.showSettingsModal = true;
    this.modalClosing = false;
    document.body.style.overflow = 'hidden';
  }

  closeSettingsModal(): void {
    this.modalClosing = true;
    setTimeout(() => {
      this.showSettingsModal = false;
      this.modalClosing = false;
      this.selectedAccount = null;
      document.body.style.overflow = '';
    }, 300);
  }

  // --- Delete Modal ---
  openDeleteModal(account: CloudAccount): void {
    this.selectedAccount = account;
    this.showDeleteModal = true;
    this.modalClosing = false;
    document.body.style.overflow = 'hidden';
  }

  closeDeleteModal(): void {
    this.modalClosing = true;
    setTimeout(() => {
      this.showDeleteModal = false;
      this.modalClosing = false;
      this.selectedAccount = null;
      document.body.style.overflow = '';
    }, 300);
  }

  confirmDelete(): void {
    // Dummy — just close for now
    this.closeDeleteModal();
  }

  // --- Settings ---
  loadSettings(): void {
    const saved = localStorage.getItem('cloudzen_settings');
    if (saved) {
      this.settings = { ...this.settings, ...JSON.parse(saved) };
    }
  }

  toggleSetting(key: string): void {
    this.settings[key] = !this.settings[key];
    localStorage.setItem('cloudzen_settings', JSON.stringify(this.settings));
  }

  // --- Helpers ---
  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
