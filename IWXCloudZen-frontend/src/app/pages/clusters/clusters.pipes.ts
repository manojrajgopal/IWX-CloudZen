import { Pipe, PipeTransform } from '@angular/core';
import { Cluster } from '../../models/cloud-services.model';

@Pipe({ name: 'clusterFilterByProvider', standalone: true })
export class ClusterFilterByProviderPipe implements PipeTransform {
  transform(clusters: Cluster[], provider: string): Cluster[] {
    if (!clusters || !provider) return [];
    return clusters.filter(c => c.provider === provider);
  }
}

@Pipe({ name: 'clusterFilterByStatus', standalone: true })
export class ClusterFilterByStatusPipe implements PipeTransform {
  transform(clusters: Cluster[], status: string): Cluster[] {
    if (!clusters || !status) return [];
    return clusters.filter(c => c.status?.toLowerCase() === status);
  }
}
