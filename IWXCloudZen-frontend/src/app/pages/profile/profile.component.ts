import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface ProfileStat {
  label: string;
  value: string;
}

interface CloudAccount {
  id: number;
  provider: string;
  accountName: string;
  region: string;
  status: 'connected' | 'disconnected' | 'syncing';
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

  profileStats: ProfileStat[] = [
    { label: 'Cloud Accounts', value: '4' },
    { label: 'Active Services', value: '12' },
    { label: 'Deployments', value: '87' },
    { label: 'Uptime', value: '99.9%' }
  ];

  cloudAccounts: CloudAccount[] = [
    {
      id: 1,
      provider: 'AWS',
      accountName: 'Production',
      region: 'ap-south-1',
      status: 'connected'
    },
    {
      id: 2,
      provider: 'AWS',
      accountName: 'Development',
      region: 'us-east-1',
      status: 'connected'
    },
    {
      id: 3,
      provider: 'Azure',
      accountName: 'Staging',
      region: 'Central India',
      status: 'syncing'
    },
    {
      id: 4,
      provider: 'GCP',
      accountName: 'Analytics',
      region: 'asia-south1',
      status: 'disconnected'
    }
  ];

  recentActivity: ActivityItem[] = [
    { icon: 'EC2', title: 'Launched 2 new EC2 instances in ap-south-1', time: '30 minutes ago' },
    { icon: 'S3', title: 'Created S3 bucket: prod-assets-042026', time: '2 hours ago' },
    { icon: 'ECS', title: 'Deployed v2.4.1 to ECS cluster', time: '5 hours ago' },
    { icon: 'IAM', title: 'Updated IAM policy for dev-team role', time: 'Yesterday' },
    { icon: 'VPC', title: 'Created new VPC peering connection', time: '2 days ago' }
  ];

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getUser();
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'connected':
        return 'bg-green-100 text-green-800';
      case 'syncing':
        return 'bg-yellow-100 text-yellow-800';
      case 'disconnected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getProviderInitial(provider: string): string {
    return provider.charAt(0);
  }
}
