import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GraphNode } from '../../../../../models/cloud-services.model';

export interface ResourceTypeConfig {
  label: string;
  icon: string;
  color: string;
  bgClass: string;
  borderClass: string;
  textClass: string;
  darkBgClass: string;
  route: string | null;
}

export const RESOURCE_TYPE_MAP: Record<string, ResourceTypeConfig> = {
  VPC: {
    label: 'VPC', icon: 'M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
    color: '#7c3aed', bgClass: 'bg-violet-50', borderClass: 'border-violet-300', textClass: 'text-violet-700', darkBgClass: 'dark-bg-violet', route: '/dashboard/vpcs'
  },
  Subnet: {
    label: 'Subnet', icon: 'M2.25 7.125C2.25 6.504 2.754 6 3.375 6h6c.621 0 1.125.504 1.125 1.125v3.75c0 .621-.504 1.125-1.125 1.125h-6a1.125 1.125 0 01-1.125-1.125v-3.75zM14.25 8.625c0-.621.504-1.125 1.125-1.125h5.25c.621 0 1.125.504 1.125 1.125v8.25c0 .621-.504 1.125-1.125 1.125h-5.25a1.125 1.125 0 01-1.125-1.125v-8.25zM3.75 16.125c0-.621.504-1.125 1.125-1.125h5.25c.621 0 1.125.504 1.125 1.125v2.25c0 .621-.504 1.125-1.125 1.125h-5.25a1.125 1.125 0 01-1.125-1.125v-2.25z',
    color: '#0891b2', bgClass: 'bg-cyan-50', borderClass: 'border-cyan-300', textClass: 'text-cyan-700', darkBgClass: 'dark-bg-cyan', route: '/dashboard/subnets'
  },
  SecurityGroup: {
    label: 'Security Group', icon: 'M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z',
    color: '#dc2626', bgClass: 'bg-red-50', borderClass: 'border-red-300', textClass: 'text-red-700', darkBgClass: 'dark-bg-red', route: '/dashboard/security-groups'
  },
  InternetGateway: {
    label: 'Internet Gateway', icon: 'M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3',
    color: '#059669', bgClass: 'bg-emerald-50', borderClass: 'border-emerald-300', textClass: 'text-emerald-700', darkBgClass: 'dark-bg-emerald', route: '/dashboard/internet-gateways'
  },
  EC2: {
    label: 'EC2 Instance', icon: 'M5.25 14.25h13.5m-13.5 0a3 3 0 01-3-3m3 3a3 3 0 100 6h13.5a3 3 0 100-6m-16.5-3a3 3 0 013-3h13.5a3 3 0 013 3m-19.5 0a4.5 4.5 0 01.9-2.7L5.737 5.1a3.375 3.375 0 012.7-1.35h7.126c1.062 0 2.062.5 2.7 1.35l2.587 3.45a4.5 4.5 0 01.9 2.7m0 0a3 3 0 01-3 3m0 3h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008zm-3 6h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008z',
    color: '#ea580c', bgClass: 'bg-orange-50', borderClass: 'border-orange-300', textClass: 'text-orange-700', darkBgClass: 'dark-bg-orange', route: '/dashboard/ec2-instances'
  },
  ECSTask: {
    label: 'ECS Task', icon: 'M21 7.5l-2.25-1.313M21 7.5v2.25m0-2.25l-2.25 1.313M3 7.5l2.25-1.313M3 7.5l2.25 1.313M3 7.5v2.25m9 3l2.25-1.313M12 12.75l-2.25-1.313M12 12.75V15m0 6.75l2.25-1.313M12 21.75V19.5m0 2.25l-2.25-1.313',
    color: '#ea580c', bgClass: 'bg-orange-50', borderClass: 'border-orange-300', textClass: 'text-orange-700', darkBgClass: 'dark-bg-orange', route: '/dashboard/ecs'
  },
  S3Bucket: {
    label: 'S3 Bucket', icon: 'M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375',
    color: '#0d9488', bgClass: 'bg-teal-50', borderClass: 'border-teal-300', textClass: 'text-teal-700', darkBgClass: 'dark-bg-teal', route: '/dashboard/cloud-storage'
  },
  S3Object: {
    label: 'S3 Object', icon: 'M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z',
    color: '#0d9488', bgClass: 'bg-teal-50', borderClass: 'border-teal-300', textClass: 'text-teal-700', darkBgClass: 'dark-bg-teal', route: null
  },
  LogGroup: {
    label: 'Log Group', icon: 'M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 010 3.75H5.625a1.875 1.875 0 010-3.75z',
    color: '#7c3aed', bgClass: 'bg-purple-50', borderClass: 'border-purple-300', textClass: 'text-purple-700', darkBgClass: 'dark-bg-purple', route: '/dashboard/cloudwatch-logs'
  },
  KeyPair: {
    label: 'Key Pair', icon: 'M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z',
    color: '#ca8a04', bgClass: 'bg-yellow-50', borderClass: 'border-yellow-300', textClass: 'text-yellow-700', darkBgClass: 'dark-bg-yellow', route: '/dashboard/key-pairs'
  },
  EICEndpoint: {
    label: 'EIC Endpoint', icon: 'M13.19 8.688a4.5 4.5 0 011.242 7.244l-4.5 4.5a4.5 4.5 0 01-6.364-6.364l1.757-1.757m13.35-.622l1.757-1.757a4.5 4.5 0 00-6.364-6.364l-4.5 4.5a4.5 4.5 0 001.242 7.244',
    color: '#2563eb', bgClass: 'bg-blue-50', borderClass: 'border-blue-300', textClass: 'text-blue-700', darkBgClass: 'dark-bg-blue', route: null
  },
  NetworkInterface: {
    label: 'Network Interface', icon: 'M8.288 15.038a5.25 5.25 0 017.424 0M5.106 11.856c3.807-3.808 9.98-3.808 13.788 0M1.924 8.674c5.565-5.565 14.587-5.565 20.152 0M12.53 18.22l-.53.53-.53-.53a.75.75 0 011.06 0z',
    color: '#6366f1', bgClass: 'bg-indigo-50', borderClass: 'border-indigo-300', textClass: 'text-indigo-700', darkBgClass: 'dark-bg-indigo', route: null
  },
  RouteTable: {
    label: 'Route Table', icon: 'M3.75 6A2.25 2.25 0 016 3.75h2.25A2.25 2.25 0 0110.5 6v2.25a2.25 2.25 0 01-2.25 2.25H6a2.25 2.25 0 01-2.25-2.25V6zM3.75 15.75A2.25 2.25 0 016 13.5h2.25a2.25 2.25 0 012.25 2.25V18a2.25 2.25 0 01-2.25 2.25H6A2.25 2.25 0 013.75 18v-2.25zM13.5 6a2.25 2.25 0 012.25-2.25H18A2.25 2.25 0 0120.25 6v2.25A2.25 2.25 0 0118 10.5h-2.25a2.25 2.25 0 01-2.25-2.25V6zM13.5 15.75a2.25 2.25 0 012.25-2.25H18a2.25 2.25 0 012.25 2.25V18A2.25 2.25 0 0118 20.25h-2.25A2.25 2.25 0 0113.5 18v-2.25z',
    color: '#64748b', bgClass: 'bg-slate-50', borderClass: 'border-slate-300', textClass: 'text-slate-700', darkBgClass: 'dark-bg-slate', route: null
  }
};

export function getResourceConfig(type: string): ResourceTypeConfig {
  return RESOURCE_TYPE_MAP[type] || {
    label: type, icon: 'M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z',
    color: '#6b7280', bgClass: 'bg-gray-50', borderClass: 'border-gray-300', textClass: 'text-gray-600', darkBgClass: 'dark-bg-gray', route: null
  };
}

export function getResourceRoute(type: string, dbId: number): string | null {
  const config = getResourceConfig(type);
  if (!config.route || !dbId) return null;
  return `${config.route}/${dbId}`;
}

export function getStateClass(state: string): { dot: string; bg: string; text: string } {
  const s = state?.toLowerCase() || '';
  if (['available', 'active', 'running', 'in-use', 'create-complete'].includes(s)) {
    return { dot: 'bg-green-500', bg: 'bg-green-50 border-green-200', text: 'text-green-700' };
  }
  if (['pending', 'creating', 'attaching', 'detaching', 'updating'].includes(s)) {
    return { dot: 'bg-yellow-500', bg: 'bg-yellow-50 border-yellow-200', text: 'text-yellow-700' };
  }
  if (['stopped', 'deleted', 'terminated', 'error', 'failed', 'detached', 'STOPPED'].includes(s)) {
    return { dot: 'bg-red-500', bg: 'bg-red-50 border-red-200', text: 'text-red-700' };
  }
  return { dot: 'bg-gray-400', bg: 'bg-gray-50 border-gray-200', text: 'text-gray-600' };
}

@Component({
  selector: 'app-graph-node',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './graph-node.component.html',
  styleUrls: ['./graph-node.component.css']
})
export class GraphNodeComponent {
  @Input() node!: GraphNode;
  @Input() expanded = false;
  @Input() childrenExpanded = false;
  @Input() highlighted = false;
  @Input() showChildren = true;
  @Input() depth = 0;

  @Output() nodeClick = new EventEmitter<GraphNode>();
  @Output() nodeExpand = new EventEmitter<GraphNode>();
  @Output() navigateToResource = new EventEmitter<{ type: string; dbId: number }>();
  @Output() navigateToGraph = new EventEmitter<{ type: string; resourceId: string; accountId?: number }>();

  get config(): ResourceTypeConfig {
    return getResourceConfig(this.node.resourceType);
  }

  get stateClass() {
    return getStateClass(this.node.state);
  }

  get displayName(): string {
    return this.node.name || this.node.resourceId || '(unnamed)';
  }

  get shortId(): string {
    const id = this.node.resourceId || '';
    if (id.length > 28) return id.substring(0, 12) + '...' + id.substring(id.length - 8);
    return id;
  }

  get resourceRoute(): string | null {
    return getResourceRoute(this.node.resourceType, this.node.dbId);
  }

  get hasChildren(): boolean {
    return this.node.children && this.node.children.length > 0;
  }

  onNodeClick(): void {
    this.nodeClick.emit(this.node);
  }

  onToggleExpand(event: Event): void {
    event.stopPropagation();
    this.nodeExpand.emit(this.node);
  }

  onNavigateResource(event: Event): void {
    event.stopPropagation();
    if (this.resourceRoute) {
      this.navigateToResource.emit({ type: this.node.resourceType, dbId: this.node.dbId });
    }
  }

  onNavigateGraph(event: Event): void {
    event.stopPropagation();
    this.navigateToGraph.emit({ type: this.node.resourceType, resourceId: this.node.resourceId });
  }
}
