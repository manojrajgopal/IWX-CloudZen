import { Pipe, PipeTransform } from '@angular/core';
import { EcsService } from '../../../models/cloud-services.model';

@Pipe({ name: 'ecsFilterByProvider', standalone: true })
export class EcsFilterByProviderPipe implements PipeTransform {
  transform(services: EcsService[], provider: string): EcsService[] {
    if (!services || !provider || provider === 'all') return services;
    return services.filter(s => s.provider === provider);
  }
}

@Pipe({ name: 'ecsFilterByStatus', standalone: true })
export class EcsFilterByStatusPipe implements PipeTransform {
  transform(services: EcsService[], status: string): EcsService[] {
    if (!services || !status || status === 'all') return services;
    return services.filter(s => s.status?.toLowerCase() === status);
  }
}

@Pipe({ name: 'ecsFilterByLaunchType', standalone: true })
export class EcsFilterByLaunchTypePipe implements PipeTransform {
  transform(services: EcsService[], launchType: string): EcsService[] {
    if (!services || !launchType || launchType === 'all') return services;
    return services.filter(s => s.launchType?.toUpperCase() === launchType.toUpperCase());
  }
}
