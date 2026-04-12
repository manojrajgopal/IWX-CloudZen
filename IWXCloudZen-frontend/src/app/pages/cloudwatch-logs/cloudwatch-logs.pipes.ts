import { Pipe, PipeTransform } from '@angular/core';
import { LogGroup } from '../../models/cloud-services.model';

@Pipe({ name: 'logGroupFilterByProvider', standalone: true })
export class LogGroupFilterByProviderPipe implements PipeTransform {
  transform(logGroups: LogGroup[], provider: string): LogGroup[] {
    if (!logGroups || !provider || provider === 'all') return logGroups;
    return logGroups.filter(lg => lg.provider === provider);
  }
}

@Pipe({ name: 'logGroupFilterByClass', standalone: true })
export class LogGroupFilterByClassPipe implements PipeTransform {
  transform(logGroups: LogGroup[], logGroupClass: string): LogGroup[] {
    if (!logGroups || !logGroupClass || logGroupClass === 'all') return logGroups;
    return logGroups.filter(lg => lg.logGroupClass === logGroupClass);
  }
}
