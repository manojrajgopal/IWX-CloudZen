import { Pipe, PipeTransform } from '@angular/core';
import { InternetGateway } from '../../../models/cloud-services.model';

@Pipe({ name: 'igwFilterByProvider', standalone: true })
export class IgwFilterByProviderPipe implements PipeTransform {
  transform(igws: InternetGateway[], provider: string): InternetGateway[] {
    if (!igws || !provider) return [];
    return igws.filter(i => i.provider === provider);
  }
}

@Pipe({ name: 'igwFilterByState', standalone: true })
export class IgwFilterByStatePipe implements PipeTransform {
  transform(igws: InternetGateway[], state: string): InternetGateway[] {
    if (!igws || !state) return [];
    return igws.filter(i => i.state?.toLowerCase() === state);
  }
}
