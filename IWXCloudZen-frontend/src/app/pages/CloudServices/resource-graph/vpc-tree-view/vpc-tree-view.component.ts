import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { GraphNode, GraphEdge, GraphSummary, FlatResource, VpcTreeResponse } from '../../../../models/cloud-services.model';
import { GraphTreeComponent } from '../shared/graph-tree/graph-tree.component';
import { getResourceConfig, getStateClass, getResourceRoute } from '../shared/graph-node/graph-node.component';

@Component({
  selector: 'app-vpc-tree-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, GraphTreeComponent],
  templateUrl: './vpc-tree-view.component.html',
  styleUrls: ['./vpc-tree-view.component.css']
})
export class VpcTreeViewComponent implements OnInit {
  loading = true;
  error: string | null = null;
  vpcId: string = '';
  accountId: number = 0;

  // Data
  vpcTree: GraphNode | null = null;
  allResources: FlatResource[] = [];
  edges: GraphEdge[] = [];
  summary: GraphSummary = {};
  hasInternetGateway = false;
  totalResources = 0;

  // View mode: db or live
  dataSource: 'db' | 'live' = 'db';
  liveLoading = false;

  // Network info
  mappedAddresses: any = null;
  networkInterfaces: any = null;
  loadingNetInfo = false;

  // Tree state
  expandedNodes = new Set<string>();
  selectedNodeId: string | null = null;
  searchQuery = '';
  collapsedTypes = new Set<string>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    this.vpcId = this.route.snapshot.paramMap.get('vpcId') || '';
    this.accountId = Number(this.route.snapshot.queryParamMap.get('accountId') || 0);

    if (!this.vpcId || !this.accountId) {
      this.error = 'Missing VPC ID or Account ID';
      this.loading = false;
      return;
    }
    this.loadTree();
  }

  loadTree(): void {
    this.loading = true;
    this.error = null;
    const req$ = this.dataSource === 'live'
      ? this.cloudServicesService.getVpcTreeLive(this.vpcId, this.accountId)
      : this.cloudServicesService.getVpcTree(this.vpcId, this.accountId);

    req$.subscribe({
      next: (res) => {
        this.vpcTree = res.vpcTree;
        this.allResources = res.allResources || [];
        this.edges = res.edges || [];
        this.summary = res.summary || {};
        this.hasInternetGateway = res.hasInternetGateway || false;
        this.totalResources = res.totalResources || 0;
        this.loading = false;
        this.autoExpand();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to load VPC tree';
        this.loading = false;
      }
    });
  }

  switchDataSource(source: 'db' | 'live'): void {
    this.dataSource = source;
    this.loadTree();
  }

  loadNetworkInfo(): void {
    this.loadingNetInfo = true;
    this.cloudServicesService.getMappedPublicAddresses(this.vpcId, this.accountId).subscribe({
      next: (res) => { this.mappedAddresses = res; },
      error: () => {}
    });
    this.cloudServicesService.getVpcNetworkInterfaces(this.vpcId, this.accountId).subscribe({
      next: (res) => { this.networkInterfaces = res; this.loadingNetInfo = false; },
      error: () => { this.loadingNetInfo = false; }
    });
  }

  private autoExpand(): void {
    this.expandedNodes.clear();
    if (this.vpcTree) {
      this.expandedNodes.add(this.vpcTree.resourceId);
      this.vpcTree.children?.forEach(c => {
        if (c.children?.length) this.expandedNodes.add(c.resourceId);
      });
    }
  }

  get treeAsArray(): GraphNode[] {
    return this.vpcTree ? [this.vpcTree] : [];
  }

  get summaryEntries() {
    return Object.entries(this.summary)
      .map(([type, count]) => ({ type, count, config: getResourceConfig(type) }))
      .sort((a, b) => b.count - a.count);
  }

  get vpcDisplayName(): string {
    return this.vpcTree?.name || this.vpcId;
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
    if (this.vpcTree) addAll([this.vpcTree]);
    this.expandedNodes = new Set(this.expandedNodes);
  }

  collapseAll(): void {
    this.expandedNodes.clear();
    this.expandedNodes = new Set();
  }

  onNavigateGraph(event: { type: string; resourceId: string }): void {
    this.router.navigate(['/dashboard/resource-graph/dependencies'], {
      queryParams: { type: event.type, id: event.resourceId, accountId: this.accountId }
    });
  }

  getResourceConfig(type: string) { return getResourceConfig(type); }
  getStateClass(state: string) { return getStateClass(state); }
  getResourceRoute(type: string, dbId: number) { return getResourceRoute(type, dbId); }
}
