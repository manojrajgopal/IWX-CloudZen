import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import {
  EcsTaskDefinition, CreateTaskDefinitionRequest,
  CreateContainerDefinition, ContainerPortMapping,
  ContainerEnvironment, ContainerLogConfiguration,
  CheckPermissionResult, LogGroup, EcrRepository
} from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

interface ContainerForm {
  name: string;
  image: string;
  imageMode: 'select' | 'manual';
  cpu: number;
  memory: number;
  memoryReservation: number | null;
  essential: boolean;
  portMappings: ContainerPortMapping[];
  environment: ContainerEnvironment[];
  enableLogging: boolean;
  logDriver: string;
  logOptions: { key: string; value: string }[];
  expanded: boolean;
}

@Component({
  selector: 'app-create-task-definition',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-task-definition.component.html',
  styleUrls: ['./create-task-definition.component.css']
})
export class CreateTaskDefinitionComponent implements OnInit {
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // Form fields
  family = '';
  cpu = '256';
  memory = '512';
  networkMode = 'awsvpc';
  executionRoleArn = '';
  taskRoleArn = '';
  requiresCompatibilities: string[] = ['FARGATE'];
  osFamily = 'LINUX';

  containers: ContainerForm[] = [];

  // IAM Roles
  iamRoles: CheckPermissionResult[] = [];
  loadingRoles = false;
  rolesError = '';
  executionRoleMode: 'select' | 'manual' = 'select';
  taskRoleMode: 'select' | 'manual' = 'select';
  selectedExecutionRoleBase = '';

  // CloudWatch Log Groups
  logGroups: LogGroup[] = [];
  loadingLogGroups = false;

  // ECR Repositories
  ecrRepositories: EcrRepository[] = [];
  loadingEcrRepos = false;

  formState: FormState = 'loading';
  createdTaskDef: EcsTaskDefinition | null = null;
  errorMessage = '';
  progress = 0;
  private progressInterval: any;

  returnTo: string | null = null;
  familyTouched = false;

  cpuOptions = ['256', '512', '1024', '2048', '4096'];
  memoryOptions: Record<string, string[]> = {
    '256': ['512', '1024', '2048'],
    '512': ['1024', '2048', '3072', '4096'],
    '1024': ['2048', '3072', '4096', '5120', '6144', '7168', '8192'],
    '2048': ['4096', '5120', '6144', '7168', '8192', '9216', '10240', '11264', '12288', '13312', '14336', '15360', '16384'],
    '4096': ['8192', '9216', '10240', '11264', '12288', '13312', '14336', '15360', '16384', '17408', '18432', '19456', '20480', '21504', '22528', '23552', '24576', '25600', '26624', '27648', '28672', '29696', '30720']
  };

  networkModes = ['awsvpc', 'bridge', 'host', 'none'];
  osFamilies = ['LINUX', 'WINDOWS_SERVER_2019_FULL', 'WINDOWS_SERVER_2019_CORE', 'WINDOWS_SERVER_2022_FULL', 'WINDOWS_SERVER_2022_CORE'];
  compatibilityOptions = ['FARGATE', 'EC2'];
  logDrivers = ['awslogs', 'fluentd', 'gelf', 'journald', 'json-file', 'splunk', 'syslog'];

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
    this.addContainer();
  }

  private loadAccounts(): void {
    this.formState = 'loading';
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts.filter(a => a.provider?.toUpperCase() === 'AWS');
        const preSelectId = this.route.snapshot.queryParamMap.get('accountId');
        if (preSelectId) {
          const found = this.accounts.find(a => a.id === +preSelectId);
          if (found) {
            this.selectedAccountId = found.id;
            this.loadIamRoles(found.id);
            this.loadLogGroups(found.id);
            this.loadEcrRepositories(found.id);
            this.prefillLogOptionsForContainers();
          }
        } else if (this.accounts.length === 1) {
          this.selectedAccountId = this.accounts[0].id;
          this.loadIamRoles(this.accounts[0].id);
          this.loadLogGroups(this.accounts[0].id);
          this.loadEcrRepositories(this.accounts[0].id);
          this.prefillLogOptionsForContainers();
        }
        this.formState = 'form';
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load cloud accounts. Please try again.';
        this.formState = 'error';
      }
    });
  }

  selectAccount(accountId: number): void {
    this.selectedAccountId = accountId;
    this.executionRoleArn = '';
    this.taskRoleArn = '';
    this.selectedExecutionRoleBase = '';
    this.executionRoleMode = 'select';
    this.taskRoleMode = 'select';
    this.loadIamRoles(accountId);
    this.loadLogGroups(accountId);
    this.loadEcrRepositories(accountId);
    this.prefillLogOptionsForContainers();
  }

  private loadIamRoles(accountId: number): void {
    this.loadingRoles = true;
    this.rolesError = '';
    this.iamRoles = [];

    this.cloudServicesService.checkPermissions(accountId, {
      actions: ['iam:ListRoles'],
      resourceArns: ['*']
    }).subscribe({
      next: (response) => {
        this.iamRoles = response.results.filter(r => r.isAllowed);
        this.loadingRoles = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.rolesError = 'Failed to load IAM roles. You can enter the ARN manually.';
        this.loadingRoles = false;
        this.executionRoleMode = 'manual';
        this.taskRoleMode = 'manual';
        this.cdr.detectChanges();
      }
    });
  }

  private loadLogGroups(accountId: number): void {
    this.loadingLogGroups = true;
    this.logGroups = [];
    this.cloudServicesService.getLogGroups(accountId).subscribe({
      next: (response) => {
        this.logGroups = response.logGroups || [];
        this.loadingLogGroups = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingLogGroups = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadEcrRepositories(accountId: number): void {
    this.loadingEcrRepos = true;
    this.ecrRepositories = [];
    this.cloudServicesService.getEcrRepositories(accountId).subscribe({
      next: (response) => {
        this.ecrRepositories = response.repositories || [];
        this.loadingEcrRepos = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingEcrRepos = false;
        this.cdr.detectChanges();
      }
    });
  }

  getRoleName(arn: string): string {
    if (!arn) return '';
    const parts = arn.split('/');
    return parts.length > 1 ? parts.slice(1).join('/') : arn;
  }

  selectExecutionRole(arn: string): void {
    this.selectedExecutionRoleBase = arn;
    this.updateExecutionRoleArn();
  }

  onFamilyChange(): void {
    if (this.executionRoleMode === 'select' && this.selectedExecutionRoleBase) {
      this.updateExecutionRoleArn();
    }
  }

  private updateExecutionRoleArn(): void {
    const base = this.selectedExecutionRoleBase;
    if (!base) return;
    const familyName = this.family.trim();
    if (base.endsWith('/')) {
      this.executionRoleArn = base + familyName;
    } else {
      this.executionRoleArn = base;
    }
  }

  selectTaskRole(arn: string): void {
    this.taskRoleArn = this.taskRoleArn === arn ? '' : arn;
  }

  private prefillLogOptionsForContainers(): void {
    const account = this.selectedAccount;
    if (!account) return;
    for (const container of this.containers) {
      const regionOpt = container.logOptions.find(o => o.key === 'awslogs-region');
      if (regionOpt && !regionOpt.value) {
        regionOpt.value = account.region || '';
      }
      const prefixOpt = container.logOptions.find(o => o.key === 'awslogs-stream-prefix');
      if (prefixOpt && !prefixOpt.value) {
        prefixOpt.value = container.name || 'ecs';
      }
    }
  }

  setLogOptionValue(container: ContainerForm, key: string, value: string): void {
    const opt = container.logOptions.find(o => o.key === key);
    if (opt) {
      opt.value = value;
    }
  }

  getLogOptionValue(container: ContainerForm, key: string): string {
    return container.logOptions.find(o => o.key === key)?.value || '';
  }

  get selectedAccount(): CloudAccount | null {
    return this.accounts.find(a => a.id === this.selectedAccountId) || null;
  }

  get availableMemory(): string[] {
    return this.memoryOptions[this.cpu] || ['512'];
  }

  onCpuChange(): void {
    const available = this.availableMemory;
    if (!available.includes(this.memory)) {
      this.memory = available[0];
    }
  }

  // ── Compatibility ──

  toggleCompatibility(value: string): void {
    const idx = this.requiresCompatibilities.indexOf(value);
    if (idx >= 0) {
      if (this.requiresCompatibilities.length > 1) {
        this.requiresCompatibilities.splice(idx, 1);
      }
    } else {
      this.requiresCompatibilities.push(value);
    }
  }

  isCompatibilitySelected(value: string): boolean {
    return this.requiresCompatibilities.includes(value);
  }

  // ── Container Management ──

  addContainer(): void {
    const account = this.selectedAccount;
    const containerName = this.containers.length === 0 ? 'app' : `container-${this.containers.length + 1}`;
    this.containers.push({
      name: containerName,
      image: '',
      imageMode: 'select',
      cpu: 256,
      memory: 512,
      memoryReservation: null,
      essential: this.containers.length === 0,
      portMappings: [],
      environment: [],
      enableLogging: true,
      logDriver: 'awslogs',
      logOptions: [
        { key: 'awslogs-group', value: '' },
        { key: 'awslogs-region', value: account?.region || '' },
        { key: 'awslogs-stream-prefix', value: containerName }
      ],
      expanded: true
    });
  }

  removeContainer(index: number): void {
    if (this.containers.length > 1) {
      this.containers.splice(index, 1);
    }
  }

  toggleContainer(index: number): void {
    this.containers[index].expanded = !this.containers[index].expanded;
  }

  // Port mapping management
  addPortMapping(container: ContainerForm): void {
    container.portMappings.push({ containerPort: 80, hostPort: 80, protocol: 'tcp' });
  }

  removePortMapping(container: ContainerForm, index: number): void {
    container.portMappings.splice(index, 1);
  }

  // Environment variable management
  addEnvVar(container: ContainerForm): void {
    container.environment.push({ name: '', value: '' });
  }

  removeEnvVar(container: ContainerForm, index: number): void {
    container.environment.splice(index, 1);
  }

  // Log option management
  addLogOption(container: ContainerForm): void {
    container.logOptions.push({ key: '', value: '' });
  }

  removeLogOption(container: ContainerForm, index: number): void {
    container.logOptions.splice(index, 1);
  }

  // ── Validation ──

  get familyError(): string | null {
    if (!this.familyTouched) return null;
    const name = this.family.trim();
    if (!name) return 'Family name is required';
    if (name.length > 255) return 'Must be 255 characters or less';
    if (!/^[a-zA-Z][a-zA-Z0-9\-_]*$/.test(name)) {
      return 'Must start with a letter; only letters, numbers, hyphens, and underscores';
    }
    return null;
  }

  get isFormValid(): boolean {
    if (!this.selectedAccountId || !this.family.trim() || this.familyError) return false;
    if (!this.executionRoleArn.trim()) return false;
    if (this.containers.length === 0) return false;
    return this.containers.every(c => c.name.trim() && c.image.trim());
  }

  // ── Create ──

  create(): void {
    if (!this.isFormValid || !this.selectedAccountId) return;

    const containerDefs: CreateContainerDefinition[] = this.containers.map(c => {
      let logConfig: ContainerLogConfiguration | null = null;
      if (c.enableLogging) {
        const opts: Record<string, string> = {};
        c.logOptions.forEach(o => { if (o.key.trim() && o.value.trim()) opts[o.key.trim()] = o.value.trim(); });
        logConfig = { logDriver: c.logDriver, options: opts };
      }
      return {
        name: c.name.trim(),
        image: c.image.trim(),
        cpu: c.cpu,
        memory: c.memory,
        memoryReservation: c.memoryReservation,
        essential: c.essential,
        portMappings: c.portMappings,
        environment: c.environment.filter(e => e.name.trim()),
        logConfiguration: logConfig
      };
    });

    const request: CreateTaskDefinitionRequest = {
      family: this.family.trim(),
      cpu: this.cpu,
      memory: this.memory,
      networkMode: this.networkMode,
      executionRoleArn: this.executionRoleArn.trim(),
      taskRoleArn: this.taskRoleArn.trim() || null,
      requiresCompatibilities: [...this.requiresCompatibilities],
      osFamily: this.osFamily,
      containerDefinitions: containerDefs
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

    this.cloudServicesService.createTaskDefinition(this.selectedAccountId, request).subscribe({
      next: (taskDef) => {
        clearInterval(this.progressInterval);
        this.progress = 100;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.createdTaskDef = taskDef;
          this.formState = 'success';
          this.cdr.detectChanges();
        }, 400);
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.progress = 0;
        this.errorMessage = err?.error?.message || err?.error?.title || 'Failed to register task definition. Please try again.';
        this.formState = 'error';
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.family = '';
    this.cpu = '256';
    this.memory = '512';
    this.networkMode = 'awsvpc';
    this.executionRoleArn = '';
    this.taskRoleArn = '';
    this.requiresCompatibilities = ['FARGATE'];
    this.osFamily = 'LINUX';
    this.containers = [];
    this.familyTouched = false;
    this.createdTaskDef = null;
    this.errorMessage = '';
    this.iamRoles = [];
    this.rolesError = '';
    this.executionRoleMode = 'select';
    this.taskRoleMode = 'select';
    this.selectedExecutionRoleBase = '';
    this.logGroups = [];
    this.ecrRepositories = [];
    this.formState = 'form';
    this.addContainer();
  }

  goBack(): void {
    if (this.returnTo) {
      this.router.navigateByUrl(this.returnTo);
    } else {
      this.router.navigate(['/dashboard/ecs']);
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

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
