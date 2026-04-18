import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'filter', standalone: true })
export class GenericFilterPipe implements PipeTransform {
  transform(items: any[], field: string, value: string): any[] {
    if (!items || !field || !value) return items || [];
    return items.filter(item => {
      const val = item[field];
      return val && val.toString().toLowerCase() === value.toLowerCase();
    });
  }
}
