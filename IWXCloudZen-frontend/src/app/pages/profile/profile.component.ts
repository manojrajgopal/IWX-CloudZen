import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CloudAccountService } from '../../services/cloud-account.service';
import { CloudAccount } from '../../models/cloud-account.model';

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
  imports: [CommonModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  currentUser: any = null;
  cloudAccounts: CloudAccount[] = [];
  loadingAccounts = true;
  accountsError = '';

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
  }

  loadCloudAccounts(): void {
    this.loadingAccounts = true;
    this.accountsError = '';
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.cloudAccounts = accounts;
        this.profileStats[0].value = accounts.length.toString();
        this.loadingAccounts = false;
      },
      error: () => {
        this.accountsError = 'Failed to load cloud accounts.';
        this.loadingAccounts = false;
      }
    });
  }

  getStatusClass(isDefault: boolean): string {
    return isDefault
      ? 'bg-green-100 text-green-800'
      : 'bg-gray-100 text-gray-800';
  }

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
