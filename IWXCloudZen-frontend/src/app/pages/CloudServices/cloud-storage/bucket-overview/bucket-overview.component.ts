import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { S3Bucket, CloudFileResponse, BucketFileSyncResponse } from '../../../../models/cloud-services.model';

type FileSortField = 'name' | 'size' | 'type' | 'folder' | 'date';
type SortDir = 'asc' | 'desc';

interface FileTypeGroup {
  type: string;
  count: number;
  totalSize: number;
  percentage: number;
}

interface FolderGroup {
  path: string;
  count: number;
  totalSize: number;
}

@Component({
  selector: 'app-bucket-overview',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './bucket-overview.component.html',
  styleUrls: ['./bucket-overview.component.css']
})
export class BucketOverviewComponent implements OnInit {
  // State
  loading = true;
  error: string | null = null;

  // Data
  bucket: S3Bucket | null = null;
  account: CloudAccount | null = null;
  files: CloudFileResponse[] = [];

  // File search & sort
  fileSearch = '';
  fileSortField: FileSortField = 'name';
  fileSortDir: SortDir = 'asc';
  selectedTypeFilter = 'all';

  // Collapsed sections
  folderSectionCollapsed = false;
  fileTypeSectionCollapsed = false;

  // Delete state
  showDeleteConfirm = false;
  deleting = false;
  deleteError: string | null = null;

  // Sync state
  syncing = false;
  syncResult: BucketFileSyncResponse | null = null;
  showSyncReport = false;
  syncError: string | null = null;

  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) {
      this.error = 'No bucket ID provided';
      this.loading = false;
      return;
    }
    const bucketId = +idParam;
    this.loadBucket(bucketId);
  }

  private loadBucket(bucketId: number): void {
    this.loading = true;
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        // Load all buckets to find the one with this ID
        const requests = accounts.map(a =>
          this.cloudServicesService.getS3Buckets(a.id).pipe(catchError(() => of({ buckets: [] })))
        );

        forkJoin(requests).subscribe(results => {
          const allBuckets = results.flatMap((r: any) => r.buckets || []);
          const found = allBuckets.find((b: S3Bucket) => b.id === bucketId);

          if (!found) {
            this.error = 'Bucket not found';
            this.loading = false;
            return;
          }

          this.bucket = found;
          this.account = accounts.find(a => a.id === found.cloudAccountId) || null;

          // Load files for this bucket
          this.cloudServicesService.getS3FilesByBucket(found.cloudAccountId, found.id)
            .pipe(catchError(() => of({ files: [] })))
            .subscribe((res: any) => {
              this.files = res.files || [];
              this.loading = false;
            });
        });
      },
      error: () => {
        this.error = 'Failed to load data';
        this.loading = false;
      }
    });
  }

  // ── Computed ──

  get totalStorage(): number {
    return this.files.reduce((sum, f) => sum + (f.size || 0), 0);
  }

  get avgFileSize(): number {
    return this.files.length > 0 ? this.totalStorage / this.files.length : 0;
  }

  get largestFile(): CloudFileResponse | null {
    if (this.files.length === 0) return null;
    return this.files.reduce((max, f) => f.size > max.size ? f : max, this.files[0]);
  }

  get fileTypeGroups(): FileTypeGroup[] {
    const map = new Map<string, { count: number; totalSize: number }>();
    for (const f of this.files) {
      const ext = this.getFileExtension(f.fileName);
      const entry = map.get(ext) || { count: 0, totalSize: 0 };
      entry.count++;
      entry.totalSize += f.size || 0;
      map.set(ext, entry);
    }
    const total = this.files.length || 1;
    return Array.from(map.entries())
      .map(([type, data]) => ({
        type,
        count: data.count,
        totalSize: data.totalSize,
        percentage: (data.count / total) * 100
      }))
      .sort((a, b) => b.count - a.count);
  }

  get uniqueFileTypes(): string[] {
    return this.fileTypeGroups.map(g => g.type);
  }

  get folderGroups(): FolderGroup[] {
    const map = new Map<string, { count: number; totalSize: number }>();
    for (const f of this.files) {
      const folder = f.folder || '/';
      const entry = map.get(folder) || { count: 0, totalSize: 0 };
      entry.count++;
      entry.totalSize += f.size || 0;
      map.set(folder, entry);
    }
    return Array.from(map.entries())
      .map(([path, data]) => ({ path, count: data.count, totalSize: data.totalSize }))
      .sort((a, b) => b.count - a.count);
  }

  get recentFiles(): CloudFileResponse[] {
    return [...this.files]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
  }

  get filteredFiles(): CloudFileResponse[] {
    let result = [...this.files];

    if (this.fileSearch.trim()) {
      const q = this.fileSearch.toLowerCase();
      result = result.filter(f =>
        f.fileName.toLowerCase().includes(q) ||
        f.folder?.toLowerCase().includes(q) ||
        f.contentType?.toLowerCase().includes(q)
      );
    }

    if (this.selectedTypeFilter !== 'all') {
      result = result.filter(f => this.getFileExtension(f.fileName) === this.selectedTypeFilter);
    }

    result.sort((a, b) => {
      let valA: string | number, valB: string | number;
      switch (this.fileSortField) {
        case 'name': valA = a.fileName; valB = b.fileName; break;
        case 'size': valA = a.size; valB = b.size;
          return this.fileSortDir === 'asc' ? (valA as number) - (valB as number) : (valB as number) - (valA as number);
        case 'type': valA = a.contentType || ''; valB = b.contentType || ''; break;
        case 'folder': valA = a.folder || ''; valB = b.folder || ''; break;
        case 'date': valA = a.createdAt || ''; valB = b.createdAt || ''; break;
        default: valA = a.fileName; valB = b.fileName;
      }
      const cmp = String(valA).localeCompare(String(valB));
      return this.fileSortDir === 'asc' ? cmp : -cmp;
    });

    return result;
  }

  // ── Actions ──

  toggleFileSort(field: FileSortField): void {
    if (this.fileSortField === field) {
      this.fileSortDir = this.fileSortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.fileSortField = field;
      this.fileSortDir = 'asc';
    }
  }

  // ── Helpers ──

  getFileExtension(filename: string): string {
    if (!filename) return 'unknown';
    const parts = filename.split('.');
    return parts.length > 1 ? parts.pop()!.toLowerCase() : 'no-ext';
  }

  getFileTypeColor(type: string): string {
    const colors: Record<string, string> = {
      'json': 'bg-amber-100 text-amber-700',
      'xml': 'bg-orange-100 text-orange-700',
      'csv': 'bg-green-100 text-green-700',
      'txt': 'bg-gray-100 text-gray-700',
      'log': 'bg-gray-100 text-gray-600',
      'png': 'bg-pink-100 text-pink-700',
      'jpg': 'bg-pink-100 text-pink-700',
      'jpeg': 'bg-pink-100 text-pink-700',
      'gif': 'bg-purple-100 text-purple-700',
      'svg': 'bg-indigo-100 text-indigo-700',
      'pdf': 'bg-red-100 text-red-700',
      'zip': 'bg-blue-100 text-blue-700',
      'gz': 'bg-blue-100 text-blue-700',
      'tar': 'bg-blue-100 text-blue-700',
      'js': 'bg-yellow-100 text-yellow-700',
      'ts': 'bg-blue-100 text-blue-700',
      'html': 'bg-orange-100 text-orange-700',
      'css': 'bg-indigo-100 text-indigo-700',
      'parquet': 'bg-teal-100 text-teal-700',
    };
    return colors[type] || 'bg-gray-100 text-gray-600';
  }

  getTypeBarColor(type: string): string {
    const colors: Record<string, string> = {
      'json': 'bg-amber-500',
      'xml': 'bg-orange-500',
      'csv': 'bg-green-500',
      'txt': 'bg-gray-500',
      'png': 'bg-pink-500',
      'jpg': 'bg-pink-400',
      'pdf': 'bg-red-500',
      'zip': 'bg-blue-500',
      'parquet': 'bg-teal-500',
      'log': 'bg-gray-400',
    };
    return colors[type] || 'bg-gray-400';
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return (bytes / Math.pow(1024, i)).toFixed(i > 0 ? 1 : 0) + ' ' + units[i];
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  getProviderLabel(provider: string): string {
    switch (provider?.toUpperCase()) {
      case 'AWS': return 'AWS S3';
      case 'AZURE': return 'Azure Blob';
      case 'GCP': return 'GCP Storage';
      default: return provider || 'Unknown';
    }
  }

  getStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'created', 'available'].includes(s)) return 'bg-green-50 text-green-700 border-green-200';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-50 text-yellow-700 border-yellow-200';
    if (['deleted', 'terminated', 'inactive', 'error'].includes(s)) return 'bg-red-50 text-red-700 border-red-200';
    return 'bg-gray-50 text-gray-600 border-gray-200';
  }

  getStatusDotClass(status: string): string {
    const s = status?.toLowerCase();
    if (['active', 'created', 'available'].includes(s)) return 'bg-green-500';
    if (['pending', 'creating', 'updating'].includes(s)) return 'bg-yellow-500';
    if (['deleted', 'terminated', 'inactive', 'error'].includes(s)) return 'bg-red-500';
    return 'bg-gray-400';
  }

  // ── Delete Bucket ──

  openDeleteConfirm(): void {
    this.showDeleteConfirm = true;
    this.deleteError = null;
  }

  closeDeleteConfirm(): void {
    this.showDeleteConfirm = false;
    this.deleteError = null;
  }

  confirmDelete(): void {
    if (!this.bucket) return;
    this.deleting = true;
    this.deleteError = null;

    this.cloudServicesService.deleteS3Bucket(this.bucket.cloudAccountId, this.bucket.id).subscribe({
      next: () => {
        this.deleting = false;
        this.showDeleteConfirm = false;
        this.router.navigate(['/dashboard/cloud-storage']);
      },
      error: (err) => {
        this.deleting = false;
        this.deleteError = err?.error?.message || err?.message || 'Failed to delete bucket. Please try again.';
      }
    });
  }

  // ── Sync Bucket Files ──

  syncBucketFiles(): void {
    if (!this.bucket || this.syncing) return;
    this.syncing = true;
    this.syncResult = null;
    this.syncError = null;
    this.showSyncReport = false;

    this.cloudServicesService.syncBucketFiles(this.bucket.cloudAccountId, this.bucket.id).subscribe({
      next: (result) => {
        this.syncing = false;
        this.syncResult = result;
        this.showSyncReport = true;
        // Update the files list with the synced files
        this.files = result.files || [];
      },
      error: (err) => {
        this.syncing = false;
        this.syncError = err?.error?.message || err?.message || 'Sync failed. Please try again.';
        setTimeout(() => { this.syncError = null; }, 5000);
      }
    });
  }

  closeSyncReport(): void {
    this.showSyncReport = false;
  }
}
