import { Pipe, PipeTransform } from '@angular/core';
import { S3Bucket } from '../../models/cloud-services.model';

@Pipe({ name: 'filterByProvider', standalone: true })
export class FilterByProviderPipe implements PipeTransform {
  transform(buckets: S3Bucket[], provider: string): S3Bucket[] {
    if (!buckets || !provider) return [];
    return buckets.filter(b => b.provider === provider);
  }
}

@Pipe({ name: 'filterByRegion', standalone: true })
export class FilterByRegionPipe implements PipeTransform {
  transform(buckets: S3Bucket[], region: string): S3Bucket[] {
    if (!buckets || !region) return [];
    return buckets.filter(b => b.region === region);
  }
}

@Pipe({ name: 'filterByStatus', standalone: true })
export class FilterByStatusPipe implements PipeTransform {
  transform(buckets: S3Bucket[], status: string): S3Bucket[] {
    if (!buckets || !status) return [];
    return buckets.filter(b => b.status?.toLowerCase() === status);
  }
}
