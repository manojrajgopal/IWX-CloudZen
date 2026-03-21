import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface Metric {
  label: string;
  value: string;
}

interface Service {
  id: number;
  title: string;
  description: string;
  icon: string;
  cloud: string;
  status: 'active' | 'pending' | 'inactive';
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  currentUser: any = null;
  metrics: Metric[] = [
    { label: 'Active Services', value: '12' },
    { label: 'Connected Clouds', value: '3' },
    { label: 'Cost Savings', value: '₹42,500' },
    { label: 'Health Score', value: '98%' }
  ];

  services: Service[] = [
    {
      id: 1,
      title: 'Kubernetes Manager',
      description: 'Manage clusters across AWS EKS, Azure AKS, GCP GKE.',
      icon: 'https://plus.unsplash.com/premium_photo-1673485529863-13770222fe0f',
      cloud: 'Multi‑Cloud',
      status: 'active'
    },
    {
      id: 2,
      title: 'Serverless Framework',
      description: 'Deploy functions to AWS Lambda, Azure Functions, GCP Cloud Functions.',
      icon: 'https://plus.unsplash.com/premium_photo-1736517545207-6a147b76c9b0',
      cloud: 'Multi‑Cloud',
      status: 'active'
    },
    {
      id: 3,
      title: 'Cost Optimizer',
      description: 'AI‑driven recommendations to reduce cloud spend.',
      icon: 'https://plus.unsplash.com/premium_photo-1772987698999-312c441f3d6d',
      cloud: 'Multi‑Cloud',
      status: 'pending'
    },
    {
      id: 4,
      title: 'Security Posture Manager',
      description: 'Continuous compliance checks and threat detection.',
      icon: 'https://plus.unsplash.com/premium_photo-1739371779873-32e0e3d224ee',
      cloud: 'Multi‑Cloud',
      status: 'active'
    }
  ];

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getUser();
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'active':
        return 'bg-green-100 text-green-800';
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }
}