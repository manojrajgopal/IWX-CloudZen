import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { GraphNode, GraphEdge, FullGraphResponse, DependencyResponse } from '../../../../models/cloud-services.model';
import { GraphTreeComponent } from '../shared/graph-tree/graph-tree.component';
import { getResourceConfig, getResourceRoute, getStateClass, RESOURCE_TYPE_MAP } from '../shared/graph-node/graph-node.component';

interface FlatResourceItem {
  resourceId: string;
  name: string;
  resourceType: string;
  state: string;
  dbId: number;
}

@Component({
  selector: 'app-dependencies-explorer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, GraphTreeComponent],
  templateUrl: './dependencies-explorer.component.html',
  styleUrls: ['./dependencies-explorer.component.css']
})
export class DependenciesExplorerComponent implements OnInit {
  // State machine: 'account' | 'type' | 'resource' | 'result'
  step: 'account' | 'type' | 'resource' | 'result' = 'account';

  // Loading
  initialLoading = true;
  loadingResources = false;
  loadingDeps = false;
  error: string | null = null;

  // Accounts
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // Resource Type Selection
  resourceTypes = Object.entries(RESOURCE_TYPE_MAP).map(([key, val]) => ({ key, ...val }));

  selectedType: string | null = null;

  // Resource Selection
  allResources: FlatResourceItem[] = [];
  filteredResources: FlatResourceItem[] = [];
  resourceSearchQuery = '';

  selectedResource: FlatResourceItem | null = null;

  // Dependency Result
  dependencies: DependencyResponse | null = null;
  depTree: GraphNode[] = [];
  depEdges: GraphEdge[] = [];

  // Tree state
  expandedNodes = new Set<string>();
  selectedNodeId: string | null = null;
  collapsedTypes = new Set<string>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudServicesService: CloudServicesService,
    private cloudAccountService: CloudAccountService
  ) {}

  ngOnInit(): void {
    // Auto-select if query params present
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
            if (preType) {
              this.selectType(preType, preId);
            }
          }
        } else if (accounts.length === 1) {
          this.selectAccount(accounts[0]);
        }
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.initialLoading = false;
      }
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

    // Load full graph to get resources
    this.cloudServicesService.getFullGraph(this.selectedAccountId!).subscribe({
      next: (res) => {
        // Flatten all resources from graph
        const flat: FlatResourceItem[] = [];
        const flatten = (nodes: GraphNode[]) => {
          nodes.forEach(n => {
            flat.push({
              resourceId: n.resourceId,
              name: n.name,
              resourceType: n.resourceType,
              state: n.state,
              dbId: n.dbId
            });
            if (n.children) flatten(n.children);
          });
        };
        flatten(res.graph);

        // Filter to selected type
        this.allResources = flat.filter(r => r.resourceType === type);
        this.filteredResources = [...this.allResources];
        this.loadingResources = false;

        // Auto-select if preId provided
        if (preId) {
          const match = this.allResources.find(r => r.resourceId === preId);
          if (match) {
            this.selectResource(match);
          }
        }
      },
      error: () => {
        this.error = 'Failed to load resources';
        this.loadingResources = false;
      }
    });
  }

  filterResources(): void {
    const q = this.resourceSearchQuery.toLowerCase();
    this.filteredResources = this.allResources.filter(r =>
      (r.name || '').toLowerCase().includes(q) ||
      r.resourceId.toLowerCase().includes(q)
    );
  }

  selectResource(resource: FlatResourceItem): void {
    this.selectedResource = resource;
    this.loadingDeps = true;
    this.step = 'result';

    this.cloudServicesService.getResourceDependencies(resource.resourceType, resource.resourceId, this.selectedAccountId!).subscribe({
      next: (res) => {
        this.dependencies = res;
        this.depTree = res.dependsOn.map(r => ({
          resourceType: r.resourceType,
          resourceId: r.resourceId,
          name: r.name,
          state: r.state,
          dbId: r.dbId,
          provider: r.provider,
          metadata: {},
          children: [],
          totalDescendants: 0
        }));
        this.depEdges = res.edges || [];
        this.loadingDeps = false;
        this.autoExpand();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to load dependencies';
        this.loadingDeps = false;
      }
    });
  }

  private autoExpand(): void {
    this.expandedNodes.clear();
    const expandLevel = (nodes: GraphNode[], depth: number) => {
      if (depth > 2) return;
      nodes.forEach(n => {
        if (n.children?.length) {
          this.expandedNodes.add(n.resourceId);
          expandLevel(n.children, depth + 1);
        }
      });
    };
    expandLevel(this.depTree, 0);
    this.expandedNodes = new Set(this.expandedNodes);
  }

  goToStep(step: 'account' | 'type' | 'resource'): void {
    this.step = step;
    this.error = null;
    if (step === 'account') {
      this.selectedAccountId = null;
      this.selectedType = null;
      this.selectedResource = null;
      this.dependencies = null;
    } else if (step === 'type') {
      this.selectedType = null;
      this.selectedResource = null;
      this.dependencies = null;
    } else if (step === 'resource') {
      this.selectedResource = null;
      this.dependencies = null;
    }
  }

  onNodeSelected(node: GraphNode): void {
    this.selectedNodeId = this.selectedNodeId === node.resourceId ? null : node.resourceId;
  }

  onNodeToggled(event: { node: GraphNode; expanded: boolean }): void {
    if (event.expanded) {
      this.expandedNodes.add(event.node.resourceId);
    } else {
      this.expandedNodes.delete(event.node.resourceId);
    }
    this.expandedNodes = new Set(this.expandedNodes);
  }

  expandAll(): void {
    const addAll = (nodes: GraphNode[]) => {
      nodes.forEach(n => {
        if (n.children?.length) {
          this.expandedNodes.add(n.resourceId);
          addAll(n.children);
        }
      });
    };
    addAll(this.depTree);
    this.expandedNodes = new Set(this.expandedNodes);
  }

  collapseAll(): void {
    this.expandedNodes.clear();
    this.expandedNodes = new Set();
  }

  onNavigateGraph(event: { type: string; resourceId: string }): void {
    // Navigate to same page with new resource
    this.router.navigate(['/dashboard/resource-graph/dependencies'], {
      queryParams: { type: event.type, id: event.resourceId, accountId: this.selectedAccountId }
    });
    // Reload the dependency for the new resource
    const match = { resourceId: event.resourceId, name: '', resourceType: event.type, state: '', dbId: 0 };
    this.selectedType = event.type;
    this.selectResource(match);
  }

  getResourceConfig(type: string) { return getResourceConfig(type); }
  getStateClass(state: string) { return getStateClass(state); }
  getResourceRoute(type: string, dbId: number) { return getResourceRoute(type, dbId); }

  get depTypes(): Record<string, number> {
    if (!this.dependencies) return {};
    const counts: Record<string, number> = {};
    [...this.dependencies.dependsOn, ...this.dependencies.dependedBy].forEach(r => {
      counts[r.resourceType] = (counts[r.resourceType] || 0) + 1;
    });
    return counts;
  }
}
