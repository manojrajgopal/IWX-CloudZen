import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { GraphNode } from '../../../../../models/cloud-services.model';
import { GraphNodeComponent, getResourceConfig } from '../graph-node/graph-node.component';

@Component({
  selector: 'app-graph-tree',
  standalone: true,
  imports: [CommonModule, GraphNodeComponent],
  templateUrl: './graph-tree.component.html',
  styleUrls: ['./graph-tree.component.css']
})
export class GraphTreeComponent {
  @Input() nodes: GraphNode[] = [];
  @Input() depth = 0;
  @Input() maxDepth = 10;
  @Input() expandedNodes: Set<string> = new Set();
  @Input() selectedNodeId: string | null = null;
  @Input() searchQuery = '';
  @Input() collapsedTypes: Set<string> = new Set();
  @Input() accountId: number | null = null;

  @Output() nodeSelected = new EventEmitter<GraphNode>();
  @Output() nodeToggled = new EventEmitter<{ node: GraphNode; expanded: boolean }>();
  @Output() navigateResource = new EventEmitter<{ type: string; dbId: number }>();
  @Output() navigateGraph = new EventEmitter<{ type: string; resourceId: string }>();

  constructor(private router: Router) {}

  get filteredNodes(): GraphNode[] {
    let result = this.nodes;
    if (this.collapsedTypes.size > 0) {
      result = result.filter(n => !this.collapsedTypes.has(n.resourceType));
    }
    return result;
  }

  isExpanded(node: GraphNode): boolean {
    return this.expandedNodes.has(node.resourceId);
  }

  isSelected(node: GraphNode): boolean {
    return this.selectedNodeId === node.resourceId;
  }

  isHighlighted(node: GraphNode): boolean {
    if (!this.searchQuery) return false;
    const q = this.searchQuery.toLowerCase();
    return (node.name?.toLowerCase().includes(q) || node.resourceId?.toLowerCase().includes(q)) || false;
  }

  onNodeClick(node: GraphNode): void {
    this.nodeSelected.emit(node);
  }

  onNodeExpand(node: GraphNode): void {
    const expanded = !this.isExpanded(node);
    this.nodeToggled.emit({ node, expanded });
  }

  onNavigateResource(event: { type: string; dbId: number }): void {
    this.navigateResource.emit(event);
    const config = getResourceConfig(event.type);
    if (config.route && event.dbId) {
      this.router.navigate([config.route, event.dbId]);
    }
  }

  onNavigateGraph(event: { type: string; resourceId: string }): void {
    this.navigateGraph.emit(event);
  }

  trackByResourceId(_index: number, node: GraphNode): string {
    return node.resourceId;
  }
}
