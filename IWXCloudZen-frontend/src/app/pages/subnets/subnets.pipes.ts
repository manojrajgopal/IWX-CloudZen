import { Pipe, PipeTransform } from '@angular/core';
import { Subnet } from '../../models/cloud-services.model';

@Pipe({ name: 'subnetFilterByProvider', standalone: true })
export class SubnetFilterByProviderPipe implements PipeTransform {
  transform(subnets: Subnet[], provider: string): Subnet[] {
    if (!subnets || !provider || provider === 'all') return subnets;
    return subnets.filter(s => s.provider === provider);
  }
}

@Pipe({ name: 'subnetFilterByState', standalone: true })
export class SubnetFilterByStatePipe implements PipeTransform {
  transform(subnets: Subnet[], state: string): Subnet[] {
    if (!subnets || !state || state === 'all') return subnets;
    return subnets.filter(s => s.state?.toLowerCase() === state);
  }
}

@Pipe({ name: 'subnetFilterByAz', standalone: true })
export class SubnetFilterByAzPipe implements PipeTransform {
  transform(subnets: Subnet[], az: string): Subnet[] {
    if (!subnets || !az || az === 'all') return subnets;
    return subnets.filter(s => s.availabilityZone === az);
  }
}
