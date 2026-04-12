import { Pipe, PipeTransform } from '@angular/core';
import { Ec2Instance } from '../../../models/cloud-services.model';

@Pipe({ name: 'ec2FilterByProvider', standalone: true })
export class Ec2FilterByProviderPipe implements PipeTransform {
  transform(instances: Ec2Instance[], provider: string): Ec2Instance[] {
    if (!instances || !provider || provider === 'all') return instances;
    return instances.filter(i => i.provider === provider);
  }
}

@Pipe({ name: 'ec2FilterByState', standalone: true })
export class Ec2FilterByStatePipe implements PipeTransform {
  transform(instances: Ec2Instance[], state: string): Ec2Instance[] {
    if (!instances || !state || state === 'all') return instances;
    return instances.filter(i => i.state?.toLowerCase() === state);
  }
}

@Pipe({ name: 'ec2FilterByType', standalone: true })
export class Ec2FilterByTypePipe implements PipeTransform {
  transform(instances: Ec2Instance[], type: string): Ec2Instance[] {
    if (!instances || !type || type === 'all') return instances;
    return instances.filter(i => i.instanceType === type);
  }
}
