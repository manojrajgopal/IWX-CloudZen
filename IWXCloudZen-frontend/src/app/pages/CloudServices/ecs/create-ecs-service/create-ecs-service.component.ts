import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import {
  EcsService, Cluster, EcsTaskDefinition, Subnet, SecurityGroup,
  CreateEcsServiceRequest
} from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

@Component({
  selector: 'app-create-ecs-service',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-ecs-service.component.html',
  styleUrls: ['./create-ecs-service.component.css']
})
export class CreateEcsServiceComponent implements OnInit {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // Related resources
  clusters: Cluster[] = [];
  taskDefinitions: EcsTaskDefinition[] = [];
  subnets: Subnet[] = [];
  securityGroups: SecurityGroup[] = [];
  resourcesLoading = false;

  // Form fields
  serviceName = '';
  selectedClusterName = '';
  selectedTaskDefinition = '';
  desiredCount = 2;
  launchType = 'FARGATE';
  schedulingStrategy = 'REPLICA';
  selectedSubnets: string[] = [];
  selectedSecurityGroups: string[] = [];
  assignPublicIp = true;

  formState: FormState = 'loading';
  createdService: EcsService | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;

  // Touched flags
  serviceNameTouched = false;

  // Dropdown states
  showClusterDropdown = false;
  showTaskDefDropdown = false;
  showSubnetDropdown = false;
  showSecurityGroupDropdown = false;

  launchTypes = ['FARGATE', 'EC2', 'EXTERNAL'];
  schedulingStrategies = ['REPLICA', 'DAEMON'];

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
        if (this.selectedAccountId) {
          this.loadResources(this.selectedAccountId);
        }
      },
      error: () => {
        this.errorMessage = 'Failed to load cloud accounts. Please try again.';
        this.formState = 'error';
      }
    });
  }

  selectAccount(accountId: number): void {
    this.selectedAccountId = accountId;
    this.selectedClusterName = '';
    this.selectedTaskDefinition = '';
    this.selectedSubnets = [];
    this.selectedSecurityGroups = [];
    this.clusters = [];
    this.taskDefinitions = [];
    this.subnets = [];
    this.securityGroups = [];
    this.loadResources(accountId);
  }

  private loadResources(accountId: number): void {
    this.resourcesLoading = true;
    forkJoin({
      clusters: this.cloudServicesService.getClusters(accountId).pipe(catchError(() => of({ clusters: [] }))),
      taskDefs: this.cloudServicesService.getEcsTaskDefinitions(accountId).pipe(catchError(() => of({ taskDefinitions: [] }))),
      subnets: this.cloudServicesService.getSubnets(accountId).pipe(catchError(() => of({ subnets: [] }))),
      securityGroups: this.cloudServicesService.getSecurityGroups(accountId).pipe(catchError(() => of({ securityGroups: [] })))
    }).subscribe({
      next: (res: any) => {
        this.clusters = res.clusters.clusters || [];
        this.taskDefinitions = res.taskDefs.taskDefinitions || [];
        this.subnets = res.subnets.subnets || [];
        this.securityGroups = res.securityGroups.securityGroups || [];
        this.resourcesLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.resourcesLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  // ── Validation ──

  get serviceNameError(): string | null {
    if (!this.serviceNameTouched) return null;
    const name = this.serviceName.trim();
    if (!name) return 'Service name is required';
    if (name.length > 255) return 'Must be 255 characters or less';
    if (!/^[a-zA-Z][a-zA-Z0-9\-_]*$/.test(name)) {
      return 'Must start with a letter; only letters, numbers, hyphens, and underscores';
    }
    return null;
  }

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      this.serviceName.trim().length >= 1 &&
      !this.serviceNameError &&
      !!this.selectedClusterName &&
      !!this.selectedTaskDefinition &&
      this.desiredCount >= 0 &&
      this.selectedSubnets.length > 0 &&
      this.selectedSecurityGroups.length > 0;
  }

  // ── Subnet / SG Selection ──

  toggleSubnet(subnetId: string): void {
    const idx = this.selectedSubnets.indexOf(subnetId);
    if (idx >= 0) {
      this.selectedSubnets.splice(idx, 1);
    } else {
      this.selectedSubnets.push(subnetId);
    }
  }

  isSubnetSelected(subnetId: string): boolean {
    return this.selectedSubnets.includes(subnetId);
  }

  toggleSecurityGroup(sgId: string): void {
    const idx = this.selectedSecurityGroups.indexOf(sgId);
    if (idx >= 0) {
      this.selectedSecurityGroups.splice(idx, 1);
    } else {
      this.selectedSecurityGroups.push(sgId);
    }
  }

  isSecurityGroupSelected(sgId: string): boolean {
    return this.selectedSecurityGroups.includes(sgId);
  }

  // ── Navigation Helpers ──

  navigateToCreateCluster(): void {
    this.router.navigate(['/dashboard/clusters/create'], {
      queryParams: { returnTo: '/dashboard/ecs/create', accountId: this.selectedAccountId }
    });
  }

  navigateToCreateTaskDef(): void {
    this.router.navigate(['/dashboard/ecs/create-task-definition'], {
      queryParams: { returnTo: '/dashboard/ecs/create', accountId: this.selectedAccountId }
    });
  }

  navigateToCreateSubnet(): void {
    this.router.navigate(['/dashboard/subnets/create'], {
      queryParams: { returnTo: '/dashboard/ecs/create', accountId: this.selectedAccountId }
    });
  }

  navigateToCreateSecurityGroup(): void {
    this.router.navigate(['/dashboard/security-groups/create'], {
      queryParams: { returnTo: '/dashboard/ecs/create', accountId: this.selectedAccountId }
    });
  }

  // ── Create ──

  create(): void {
    if (!this.isFormValid || !this.selectedAccountId) return;

    const request: CreateEcsServiceRequest = {
      serviceName: this.serviceName.trim(),
      clusterName: this.selectedClusterName,
      taskDefinition: this.selectedTaskDefinition,
      desiredCount: this.desiredCount,
      launchType: this.launchType,
      schedulingStrategy: this.schedulingStrategy,
      networkConfiguration: {
        subnets: [...this.selectedSubnets],
        securityGroups: [...this.selectedSecurityGroups],
        assignPublicIp: this.assignPublicIp
      }
    };

    this.formState = 'creating';
    this.progress = 0;
    this.errorMessage = '';

    this.progressInterval = setInterval(() => {
      if (this.progress < 85) {
        this.progress += Math.random() * 6 + 2;
        this.progress = Math.min(this.progress, 85);
        this.cdr.detectChanges();
      }
    }, 200);

    this.cloudServicesService.createEcsService(this.selectedAccountId, request).subscribe({
      next: (service) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdService = service;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to create ECS service. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.serviceName = '';
    this.selectedClusterName = '';
    this.selectedTaskDefinition = '';
    this.desiredCount = 2;
    this.launchType = 'FARGATE';
    this.schedulingStrategy = 'REPLICA';
    this.selectedSubnets = [];
    this.selectedSecurityGroups = [];
    this.assignPublicIp = true;
    this.serviceNameTouched = false;
    this.createdService = null;
    this.errorMessage = '';
    this.formState = 'form';
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/ecs']);
    }
  }

  goToService(): void {
    if (this.createdService) {
      this.router.navigate(['/dashboard/ecs', this.createdService.id]);
    }
  }

  goToDashboard(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/ecs']);
    }
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to ECS Services';
    const segments = this.returnTo.replace(/^\//, '').split('/').filter(s => s && s !== 'dashboard');
    if (segments.length === 0) return 'Back';
    const label = segments
      .map(s => s.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '))
      .join(' › ');
    return `Back to ${label}`;
  }

  getTaskDefLabel(td: EcsTaskDefinition): string {
    return `${td.family}:${td.revision}`;
  }

  getSubnetLabel(subnet: Subnet): string {
    const name = subnet.name?.trim() ? subnet.name.trim() : subnet.subnetId;
    return `${name} (${subnet.cidrBlock})`;
  }

  getSgLabel(sg: SecurityGroup): string {
    return `${sg.groupName} (${sg.securityGroupId})`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
