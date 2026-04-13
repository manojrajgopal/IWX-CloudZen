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
  ContainerEnvironment, ContainerLogConfiguration
} from '../../../../models/cloud-services.model';

type FormState = 'loading' | 'form' | 'creating' | 'success' | 'error';

interface ContainerForm {
  name: string;
  image: string;
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
          if (found) this.selectedAccountId = found.id;
        } else if (this.accounts.length === 1) {
          this.selectedAccountId = this.accounts[0].id;
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
    this.containers.push({
      name: this.containers.length === 0 ? 'app' : `container-${this.containers.length + 1}`,
      image: '',
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
        { key: 'awslogs-region', value: '' },
        { key: 'awslogs-stream-prefix', value: '' }
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
