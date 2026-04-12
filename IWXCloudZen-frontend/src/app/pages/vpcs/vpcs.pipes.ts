import { Pipe, PipeTransform } from '@angular/core';
import { Vpc } from '../../models/cloud-services.model';

@Pipe({ name: 'vpcFilterByProvider', standalone: true })
export class VpcFilterByProviderPipe implements PipeTransform {
  transform(vpcs: Vpc[], provider: string): Vpc[] {
    if (!vpcs || !provider) return [];
    return vpcs.filter(v => v.provider === provider);
  }
}

@Pipe({ name: 'vpcFilterByState', standalone: true })
export class VpcFilterByStatePipe implements PipeTransform {
  transform(vpcs: Vpc[], state: string): Vpc[] {
    if (!vpcs || !state) return [];
    return vpcs.filter(v => v.state?.toLowerCase() === state);
  }
}
