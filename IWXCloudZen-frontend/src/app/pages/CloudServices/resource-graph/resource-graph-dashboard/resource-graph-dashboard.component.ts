import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { GraphNode, GraphEdge, GraphSummary, FullGraphResponse } from '../../../../models/cloud-services.model';
import { GraphTreeComponent } from '../shared/graph-tree/graph-tree.component';
import { getResourceConfig, getResourceRoute, getStateClass, RESOURCE_TYPE_MAP, ResourceTypeConfig } from '../shared/graph-node/graph-node.component';

@Component({
  selector: 'app-resource-graph-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, GraphTreeComponent],
  templateUrl: './resource-graph-dashboard.component.html',
  styleUrls: ['./resource-graph-dashboard.component.css']
})
export class ResourceGraphDashboardComponent implements OnInit {
  // Loading
  initialLoading = true;
  graphLoading = false;
  syncing = false;
  error: string | null = null;

  // Account selection
  accounts: CloudAccount[] = [];
  selectedAccountId: number | null = null;

  // Graph data
  graphNodes: GraphNode[] = [];
  edges: GraphEdge[] = [];
  summary: GraphSummary = {};
  totalResources = 0;

  // Tree state
  expandedNodes = new Set<string>();
  selectedNodeId: string | null = null;
  selectedNode: GraphNode | null = null;

  // Filters
  searchQuery = '';
  collapsedTypes = new Set<string>();
  viewMode: 'tree' | 'flat' | 'summary' = 'tree';

  // Flat view pagination
  flatDisplayLimit = 200;

  // Sync report
  syncResult: any = null;
  showSyncReport = false;

  constructor(
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
  }

  private loadAccounts(): void {
    this.initialLoading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        if (accounts.length === 1) {
          this.selectAccount(accounts[0]);
        }
        this.initialLoading = false;
      },
      error: () => {
        this.error = 'Failed to load cloud accounts';
        this.initialLoading = false;
      }
    });
  }

  selectAccount(account: CloudAccount): void {
    this.selectedAccountId = account.id;
    this.loadGraph();
  }

  loadGraph(): void {
    if (!this.selectedAccountId) return;
    this.graphLoading = true;
    this.error = null;
    this.selectedNode = null;
    this.selectedNodeId = null;

    this.cloudServicesService.getFullGraph(this.selectedAccountId).subscribe({
      next: (res) => {
        this.graphNodes = res.graph || [];
        this.edges = res.edges || [];
        this.summary = res.summary || {};
        this.totalResources = res.totalResources || 0;
        this.graphLoading = false;
        this.autoExpandTopLevel();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to load resource graph';
        this.graphLoading = false;
      }
    });
  }

  private autoExpandTopLevel(): void {
    this.expandedNodes.clear();
    this.graphNodes.forEach(n => {
      if (n.children?.length > 0) {
        this.expandedNodes.add(n.resourceId);
      }
    });

    // Auto-collapse S3Object to avoid 15k+ items degrading UX
    if (this.summary['S3Object'] && this.summary['S3Object'] > 500) {
      this.collapsedTypes.add('S3Object');
      this.collapsedTypes = new Set(this.collapsedTypes);
    }
  }

  // ── Sync ──

  syncGraph(): void {
    if (!this.selectedAccountId || this.syncing) return;
    this.syncing = true;
    this.syncResult = null;
    this.showSyncReport = false;

    this.cloudServicesService.syncGraph(this.selectedAccountId).subscribe({
      next: (res) => {
        this.syncResult = res;
        this.showSyncReport = true;
        this.syncing = false;
        this.loadGraph();
      },
      error: () => {
        this.syncing = false;
      }
    });
  }

  // ── Tree Interactions ──

  onNodeSelected(node: GraphNode): void {
    this.selectedNodeId = this.selectedNodeId === node.resourceId ? null : node.resourceId;
    this.selectedNode = this.selectedNodeId ? node : null;
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
    addAll(this.graphNodes);
    this.expandedNodes = new Set(this.expandedNodes);
  }

  collapseAll(): void {
    this.expandedNodes.clear();
    this.expandedNodes = new Set();
  }

  // ── Filters ──

  toggleTypeFilter(type: string): void {
    if (this.collapsedTypes.has(type)) {
      this.collapsedTypes.delete(type);
    } else {
      this.collapsedTypes.add(type);
    }
    this.collapsedTypes = new Set(this.collapsedTypes);
  }

  isTypeVisible(type: string): boolean {
    return !this.collapsedTypes.has(type);
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.collapsedTypes.clear();
    this.collapsedTypes = new Set();
  }

  // ── Navigation ──

  navigateToVpcTree(vpcResourceId: string): void {
    if (this.selectedAccountId) {
      this.router.navigate(['/dashboard/resource-graph/vpc', vpcResourceId], {
        queryParams: { accountId: this.selectedAccountId }
      });
    }
  }

  navigateToDependencies(resourceType: string, resourceId: string): void {
    if (this.selectedAccountId) {
      this.router.navigate(['/dashboard/resource-graph/dependencies'], {
        queryParams: { type: resourceType, id: resourceId, accountId: this.selectedAccountId }
      });
    }
  }

  navigateToDeletionBlockers(resourceType: string, resourceId: string): void {
    if (this.selectedAccountId) {
      this.router.navigate(['/dashboard/resource-graph/deletion-blockers'], {
        queryParams: { type: resourceType, id: resourceId, accountId: this.selectedAccountId }
      });
    }
  }

  onNavigateGraph(event: { type: string; resourceId: string }): void {
    if (event.type === 'VPC') {
      this.navigateToVpcTree(event.resourceId);
    } else {
      this.navigateToDependencies(event.type, event.resourceId);
    }
  }

  // ── Helpers ──

  get summaryEntries(): { type: string; count: number; config: ResourceTypeConfig }[] {
    return Object.entries(this.summary)
      .map(([type, count]) => ({ type, count, config: getResourceConfig(type) }))
      .sort((a, b) => b.count - a.count);
  }

  get topLevelTypes(): string[] {
    const types = new Set<string>();
    const collect = (nodes: GraphNode[]) => {
      nodes.forEach(n => {
        types.add(n.resourceType);
        if (n.children?.length) collect(n.children);
      });
    };
    collect(this.graphNodes);
    return Array.from(types);
  }

  get flatResources(): GraphNode[] {
    const flat: GraphNode[] = [];
    const collect = (nodes: GraphNode[]) => {
      nodes.forEach(n => {
        flat.push(n);
        if (n.children?.length) collect(n.children);
      });
    };
    collect(this.graphNodes);

    // Filter out collapsed types (e.g. S3Object with 15k+ items)
    const visible = this.collapsedTypes.size > 0
      ? flat.filter(n => !this.collapsedTypes.has(n.resourceType))
      : flat;

    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      return visible.filter(n =>
        n.name?.toLowerCase().includes(q) ||
        n.resourceId?.toLowerCase().includes(q) ||
        n.resourceType?.toLowerCase().includes(q)
      );
    }
    return visible;
  }

  get edgesByRelationship(): { relationship: string; count: number }[] {
    const map = new Map<string, number>();
    this.edges.forEach(e => {
      map.set(e.relationship, (map.get(e.relationship) || 0) + 1);
    });
    return Array.from(map.entries())
      .map(([relationship, count]) => ({ relationship, count }))
      .sort((a, b) => b.count - a.count);
  }

  getResourceConfig(type: string) { return getResourceConfig(type); }
  getStateClass(state: string) { return getStateClass(state); }
  getResourceRoute(type: string, dbId: number) { return getResourceRoute(type, dbId); }
}
