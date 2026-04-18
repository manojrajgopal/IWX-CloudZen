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
  Cluster, EcsTaskDefinition, Subnet, SecurityGroup, EcsTask,
  RunTaskRequest, RunTaskNetworkConfig, RunTaskEnvironmentOverride
} from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'running' | 'success' | 'error';

interface EnvOverrideRow {
  containerName: string;
  envVars: { name: string; value: string }[];
}

@Component({
  selector: 'app-ecs-run-task',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ecs-run-task.component.html',
  styleUrls: ['./ecs-run-task.component.css']
})
export class EcsRunTaskComponent implements OnInit {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // Related resources
  clusters: Cluster[] = [];
  taskDefinitions: EcsTaskDefinition[] = [];
  subnets: Subnet[] = [];
  securityGroups: SecurityGroup[] = [];
  resourcesLoading = false;

  // Form fields
  selectedClusterName = '';
  selectedTaskDefinition = '';
  launchType = 'FARGATE';
  taskCount = 1;
  selectedSubnets: string[] = [];
  selectedSecurityGroups: string[] = [];
  assignPublicIp = true;
  environmentOverrides: EnvOverrideRow[] = [];

  formState: FormState = 'loading';
  launchedTasks: EcsTask[] = [];
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;

  launchTypes = ['FARGATE', 'EC2', 'EXTERNAL'];

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
    this.environmentOverrides = [];
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

  get selectedTaskDef(): EcsTaskDefinition | null {
    return this.taskDefinitions.find(td => td.family === this.selectedTaskDefinition) || null;
  }

  // ── Containers from selected task definition ──

  get containerNames(): string[] {
    const td = this.selectedTaskDef;
    if (!td || !td.containerDefinitionsJson) return [];
    try {
      const defs = JSON.parse(td.containerDefinitionsJson);
      return defs.map((d: any) => d.name || d.Name).filter(Boolean);
    } catch {
      return [];
    }
  }

  onTaskDefinitionChanged(): void {
    this.environmentOverrides = [];
  }

  // ── Environment Overrides ──

  addEnvOverride(): void {
    const names = this.containerNames;
    this.environmentOverrides.push({
      containerName: names.length === 1 ? names[0] : '',
      envVars: [{ name: '', value: '' }]
    });
  }

  removeEnvOverride(idx: number): void {
    this.environmentOverrides.splice(idx, 1);
  }

  addEnvVar(overrideIdx: number): void {
    this.environmentOverrides[overrideIdx].envVars.push({ name: '', value: '' });
  }

  removeEnvVar(overrideIdx: number, varIdx: number): void {
    this.environmentOverrides[overrideIdx].envVars.splice(varIdx, 1);
    if (this.environmentOverrides[overrideIdx].envVars.length === 0) {
      this.removeEnvOverride(overrideIdx);
    }
  }

  // ── Subnet / SG Selection ──

  toggleSubnet(subnetId: string): void {
    const idx = this.selectedSubnets.indexOf(subnetId);
    if (idx >= 0) {
      this.selectedSubnets.splice(idx, 1);
    } else {
      this.selectedSubnets.push(subnetId);
    }
    const validVpcIds = this.selectedSubnetVpcIds;
    this.selectedSecurityGroups = this.selectedSecurityGroups.filter(sgId => {
      const sg = this.securityGroups.find(s => s.securityGroupId === sgId);
      return sg ? validVpcIds.has(sg.vpcId) : false;
    });
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

  get selectedSubnetVpcIds(): Set<string> {
    const vpcIds = new Set<string>();
    for (const subnetId of this.selectedSubnets) {
      const subnet = this.subnets.find(s => s.subnetId === subnetId);
      if (subnet) vpcIds.add(subnet.vpcId);
    }
    return vpcIds;
  }

  get filteredSecurityGroups(): SecurityGroup[] {
    if (this.selectedSubnets.length === 0) return this.securityGroups;
    const vpcIds = this.selectedSubnetVpcIds;
    return this.securityGroups.filter(sg => vpcIds.has(sg.vpcId));
  }

  // ── Validation ──

  get isFormValid(): boolean {
    return !!this.selectedAccountId &&
      !!this.selectedClusterName &&
      !!this.selectedTaskDefinition &&
      this.taskCount >= 1 && this.taskCount <= 10 &&
      this.selectedSubnets.length > 0 &&
      this.selectedSecurityGroups.length > 0;
  }

  // ── Submit ──

  runTask(): void {
    if (!this.isFormValid || !this.selectedAccountId) return;

    const overrides: RunTaskEnvironmentOverride[] = this.environmentOverrides
      .filter(o => o.containerName && o.envVars.some(v => v.name.trim()))
      .map(o => ({
        containerName: o.containerName,
        environment: o.envVars.filter(v => v.name.trim()).map(v => ({ name: v.name.trim(), value: v.value }))
      }));

    const request: RunTaskRequest = {
      clusterName: this.selectedClusterName,
      taskDefinition: this.selectedTaskDefinition,
      launchType: this.launchType,
      count: this.taskCount,
      networkConfiguration: {
        subnets: [...this.selectedSubnets],
        securityGroups: [...this.selectedSecurityGroups],
        assignPublicIp: this.assignPublicIp
      },
      environmentOverrides: overrides
    };

    this.formState = 'running';
    this.progress = 0;
    this.errorMessage = '';

    this.progressInterval = setInterval(() => {
      if (this.progress < 85) {
        this.progress += Math.random() * 6 + 2;
        this.progress = Math.min(this.progress, 85);
        this.cdr.detectChanges();
      }
    }, 200);

    this.cloudServicesService.runTask(this.selectedAccountId, request).subscribe({
      next: (tasks) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.launchedTasks = tasks;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to run task. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.selectedClusterName = '';
    this.selectedTaskDefinition = '';
    this.taskCount = 1;
    this.launchType = 'FARGATE';
    this.selectedSubnets = [];
    this.selectedSecurityGroups = [];
    this.assignPublicIp = true;
    this.environmentOverrides = [];
    this.launchedTasks = [];
    this.errorMessage = '';
    this.formState = 'form';
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/ecs/tasks']);
    }
  }

  goToTasks(): void {
    this.router.navigate(['/dashboard/ecs/tasks']);
  }

  get backLabel(): string {
    if (!this.returnTo) return 'Back to Tasks';
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

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  getTaskShortId(arn: string): string {
    if (!arn) return '—';
    const parts = arn.split('/');
    return parts[parts.length - 1] || arn;
  }
}
