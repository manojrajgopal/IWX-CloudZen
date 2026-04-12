import { Pipe, PipeTransform } from '@angular/core';
import { EcrRepository } from '../../models/cloud-services.model';

@Pipe({ name: 'ecrFilterByProvider', standalone: true })
export class EcrFilterByProviderPipe implements PipeTransform {
  transform(repos: EcrRepository[], provider: string): EcrRepository[] {
    if (!repos || !provider) return [];
    return repos.filter(r => r.provider === provider);
  }
}
