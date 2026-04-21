import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { GraphNode, FlatResource, DeletionBlockersResponse } from '../../../../models/cloud-services.model';
import { getResourceConfig, getResourceRoute, getStateClass, RESOURCE_TYPE_MAP } from '../shared/graph-node/graph-node.component';

interface FlatResourceItem {
  resourceId: string;
  name: string;
  resourceType: string;
  state: string;
  dbId: number;
}

@Component({
  selector: 'app-deletion-blockers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './deletion-blockers.component.html',
  styleUrls: ['./deletion-blockers.component.css']
})
export class DeletionBlockersComponent implements OnInit {
  step: 'account' | 'type' | 'resource' | 'result' = 'account';

  initialLoading = true;
  loadingResources = false;
  loadingBlockers = false;
  error: string | null = null;

  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  resourceTypes = Object.entries(RESOURCE_TYPE_MAP).map(([key, val]) => ({ key, ...val }));
  selectedType: string | null = null;

  allResources: FlatResourceItem[] = [];
  filteredResources: FlatResourceItem[] = [];
  resourceSearchQuery = '';
  selectedResource: FlatResourceItem | null = null;

  // Result
  result: DeletionBlockersResponse | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudServicesService: CloudServicesService,
    private cloudAccountService: CloudAccountService
  ) {}

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParams;
    const preAccountId = qp['accountId'] ? Number(qp['accountId']) : null;
    const preType = qp['type'] || null;
    const preId = qp['id'] || null;
    this.loadAccounts(preAccountId, preType, preId);
  }

  loadAccounts(preAccountId?: number | null, preType?: string | null, preId?: string | null): void {
    this.initialLoading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.initialLoading = false;
        if (preAccountId) {
          const match = accounts.find(a => a.id === preAccountId);
          if (match) {
            this.selectAccount(match);
            if (preType) this.selectType(preType, preId);
          }
        } else if (accounts.length === 1) {
          this.selectAccount(accounts[0]);
        }
      },
      error: () => { this.error = 'Failed to load accounts'; this.initialLoading = false; }
    });
  }

  selectAccount(account: CloudAccount): void {
    this.selectedAccountId = account.id;
    this.step = 'type';
  }

  selectType(type: string, preId?: string | null): void {
    this.selectedType = type;
    this.loadingResources = true;
    this.step = 'resource';

    this.cloudServicesService.getFullGraph(this.selectedAccountId!).subscribe({
      next: (res) => {
        const flat: FlatResourceItem[] = [];
        const flatten = (nodes: GraphNode[]) => {
          nodes.forEach(n => {
            flat.push({ resourceId: n.resourceId, name: n.name, resourceType: n.resourceType, state: n.state, dbId: n.dbId });
            if (n.children) flatten(n.children);
          });
        };
        flatten(res.graph);
        this.allResources = flat.filter(r => r.resourceType === type);
        this.filteredResources = [...this.allResources];
        this.loadingResources = false;
        if (preId) {
          const match = this.allResources.find(r => r.resourceId === preId);
          if (match) this.selectResource(match);
        }
      },
      error: () => { this.error = 'Failed to load resources'; this.loadingResources = false; }
    });
  }

  filterResources(): void {
    const q = this.resourceSearchQuery.toLowerCase();
    this.filteredResources = this.allResources.filter(r =>
      (r.name || '').toLowerCase().includes(q) || r.resourceId.toLowerCase().includes(q)
    );
  }

  selectResource(resource: FlatResourceItem): void {
    this.selectedResource = resource;
    this.loadingBlockers = true;
    this.step = 'result';

    this.cloudServicesService.getDeletionBlockers(resource.resourceType, resource.resourceId, this.selectedAccountId!).subscribe({
      next: (res) => { this.result = res; this.loadingBlockers = false; },
      error: (err) => { this.error = err?.error?.message || 'Failed to load deletion blockers'; this.loadingBlockers = false; }
    });
  }

  goToStep(step: 'account' | 'type' | 'resource'): void {
    this.step = step;
    this.error = null;
    if (step === 'account') { this.selectedAccountId = null; this.selectedType = null; this.selectedResource = null; this.result = null; }
    else if (step === 'type') { this.selectedType = null; this.selectedResource = null; this.result = null; }
    else if (step === 'resource') { this.selectedResource = null; this.result = null; }
  }

  navigateToDependencies(resource: FlatResource): void {
    this.router.navigate(['/dashboard/resource-graph/dependencies'], {
      queryParams: { type: resource.resourceType, id: resource.resourceId, accountId: this.selectedAccountId }
    });
  }

  getResourceConfig(type: string) { return getResourceConfig(type); }
  getStateClass(state: string) { return getStateClass(state); }
  getResourceRoute(type: string, dbId: number) { return getResourceRoute(type, dbId); }
}
