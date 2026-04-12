import { Pipe, PipeTransform } from '@angular/core';
import { SecurityGroup } from '../../models/cloud-services.model';

@Pipe({ name: 'sgFilterByProvider', standalone: true })
export class SgFilterByProviderPipe implements PipeTransform {
  transform(groups: SecurityGroup[], provider: string): SecurityGroup[] {
    if (!groups || !provider || provider === 'all') return groups;
    return groups.filter(sg => sg.provider === provider);
  }
}
