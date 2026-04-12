import { Component, OnInit, OnDestroy } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
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
export class BucketOverviewComponent implements OnInit, OnDestroy {
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

  // Upload state
  showUploadModal = false;
  uploadFile: File | null = null;
  uploadFolder = '';
  uploading = false;
  uploadError: string | null = null;
  uploadDragOver = false;

  // File delete state
  showFileDeleteConfirm = false;
  fileToDelete: CloudFileResponse | null = null;
  deletingFile = false;
  fileDeleteError: string | null = null;

  // File update (replace) state
  showUpdateModal = false;
  fileToUpdate: CloudFileResponse | null = null;
  updateFile: File | null = null;
  updatingFile = false;
  updateFileError: string | null = null;

  // Download state
  downloadingFileId: number | null = null;

  // File detail panel
  selectedFile: CloudFileResponse | null = null;
  showFilePanel = false;
  filePreviewUrl: string | null = null;
  filePreviewSafeUrl: SafeResourceUrl | null = null;
  filePreviewText: string | null = null;
  filePreviewLoading = false;
  filePreviewError = false;

  // Toast
  toastMessage: string | null = null;
  toastType: 'success' | 'error' = 'success';

  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

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

  openFileDetail(file: CloudFileResponse): void {
    this.selectedFile = file;
    this.showFilePanel = true;
    document.body.style.overflow = 'hidden';
    this.loadFilePreview(file);
  }

  closeFileDetail(): void {
    this.showFilePanel = false;
    document.body.style.overflow = '';
    if (this.filePreviewUrl) {
      URL.revokeObjectURL(this.filePreviewUrl);
    }
    setTimeout(() => {
      this.selectedFile = null;
      this.filePreviewUrl = null;
      this.filePreviewSafeUrl = null;
      this.filePreviewText = null;
      this.filePreviewLoading = false;
      this.filePreviewError = false;
    }, 300);
  }

  loadFilePreview(file: CloudFileResponse): void {
    this.filePreviewUrl = null;
    this.filePreviewText = null;
    this.filePreviewLoading = true;
    this.filePreviewError = false;

    const ext = this.getFileExtension(this.getFileName(file));
    if (!this.isPreviewableFile(ext)) {
      this.filePreviewLoading = false;
      return;
    }

    this.cloudServicesService.downloadS3File(file.id).subscribe({
      next: (blob) => {
        if (this.isTextFile(ext)) {
          blob.text().then(text => {
            this.filePreviewText = text;
            this.filePreviewLoading = false;
          });
        } else {
          this.filePreviewUrl = URL.createObjectURL(blob);
          if (this.isPdfFile(ext)) {
            this.filePreviewSafeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.filePreviewUrl);
          }
          this.filePreviewLoading = false;
        }
      },
      error: () => {
        this.filePreviewLoading = false;
        this.filePreviewError = true;
      }
    });
  }

  isImageFile(ext: string): boolean {
    return ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp', 'ico'].includes(ext);
  }

  isTextFile(ext: string): boolean {
    return ['txt', 'json', 'xml', 'csv', 'yaml', 'yml', 'md', 'html', 'css', 'js', 'ts', 'log', 'ini', 'cfg', 'env', 'sh', 'py', 'java', 'c', 'cpp', 'h', 'rb', 'go', 'rs', 'sql', 'graphql', 'toml', 'properties'].includes(ext);
  }

  isPdfFile(ext: string): boolean {
    return ext === 'pdf';
  }

  isVideoFile(ext: string): boolean {
    return ['mp4', 'webm', 'ogg'].includes(ext);
  }

  isAudioFile(ext: string): boolean {
    return ['mp3', 'wav', 'ogg', 'aac', 'flac'].includes(ext);
  }

  isPreviewableFile(ext: string): boolean {
    return this.isImageFile(ext) || this.isTextFile(ext) || this.isPdfFile(ext) || this.isVideoFile(ext) || this.isAudioFile(ext);
  }

  openPreviewInNewTab(): void {
    if (this.filePreviewUrl) {
      window.open(this.filePreviewUrl, '_blank');
    } else if (this.filePreviewText && this.selectedFile) {
      const ext = this.getFileExtension(this.getFileName(this.selectedFile));
      const mimeMap: Record<string, string> = {
        'json': 'application/json', 'xml': 'application/xml', 'html': 'text/html',
        'css': 'text/css', 'js': 'application/javascript', 'csv': 'text/csv',
        'svg': 'image/svg+xml', 'md': 'text/markdown',
      };
      const mime = mimeMap[ext] || 'text/plain';
      const blob = new Blob([this.filePreviewText], { type: mime });
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
    }
  }

  getFileName(file: CloudFileResponse): string {
    if (file.fileName) return file.fileName;
    if (file.fileUrl) {
      const parts = file.fileUrl.split('/');
      return parts[parts.length - 1] || file.fileUrl;
    }
    return '—';
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

  // ── Upload File ──

  openUploadModal(): void {
    this.showUploadModal = true;
    this.uploadFile = null;
    this.uploadFolder = '';
    this.uploadError = null;
    this.uploadDragOver = false;
  }

  closeUploadModal(): void {
    this.showUploadModal = false;
    this.uploadFile = null;
    this.uploadFolder = '';
    this.uploadError = null;
  }

  onUploadFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile = input.files[0];
      this.uploadError = null;
    }
  }

  onUploadDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.uploadDragOver = true;
  }

  onUploadDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.uploadDragOver = false;
  }

  onUploadDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.uploadDragOver = false;
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.uploadFile = event.dataTransfer.files[0];
      this.uploadError = null;
    }
  }

  removeUploadFile(): void {
    this.uploadFile = null;
  }

  confirmUpload(): void {
    if (!this.bucket || !this.uploadFile) return;
    this.uploading = true;
    this.uploadError = null;

    this.cloudServicesService.uploadS3File(
      this.bucket.cloudAccountId,
      this.bucket.id,
      this.uploadFile,
      this.uploadFolder
    ).subscribe({
      next: (file) => {
        this.uploading = false;
        this.files = [...this.files, file];
        this.closeUploadModal();
        this.showToast('File uploaded successfully', 'success');
      },
      error: (err) => {
        this.uploading = false;
        this.uploadError = err?.error?.message || err?.message || 'Upload failed. Please try again.';
      }
    });
  }

  // ── Download File ──

  downloadFile(file: CloudFileResponse): void {
    if (this.downloadingFileId) return;
    this.downloadingFileId = file.id;

    this.cloudServicesService.downloadS3File(file.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = file.fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.downloadingFileId = null;
      },
      error: () => {
        this.downloadingFileId = null;
        this.showToast('Download failed. Please try again.', 'error');
      }
    });
  }

  // ── Update (Replace) File ──

  openUpdateModal(file: CloudFileResponse): void {
    this.showUpdateModal = true;
    this.fileToUpdate = file;
    this.updateFile = null;
    this.updateFileError = null;
  }

  closeUpdateModal(): void {
    this.showUpdateModal = false;
    this.fileToUpdate = null;
    this.updateFile = null;
    this.updateFileError = null;
  }

  onUpdateFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.updateFile = input.files[0];
      this.updateFileError = null;
    }
  }

  confirmUpdateFile(): void {
    if (!this.fileToUpdate || !this.updateFile) return;
    this.updatingFile = true;
    this.updateFileError = null;

    this.cloudServicesService.updateS3File(this.fileToUpdate.id, this.updateFile).subscribe({
      next: (updated) => {
        this.updatingFile = false;
        const idx = this.files.findIndex(f => f.id === updated.id);
        if (idx >= 0) {
          this.files = [...this.files.slice(0, idx), updated, ...this.files.slice(idx + 1)];
        }
        this.closeUpdateModal();
        this.showToast('File replaced successfully', 'success');
      },
      error: (err) => {
        this.updatingFile = false;
        this.updateFileError = err?.error?.message || err?.message || 'Update failed. Please try again.';
      }
    });
  }

  // ── Delete File ──

  openFileDeleteConfirm(file: CloudFileResponse): void {
    this.showFileDeleteConfirm = true;
    this.fileToDelete = file;
    this.fileDeleteError = null;
  }

  closeFileDeleteConfirm(): void {
    this.showFileDeleteConfirm = false;
    this.fileToDelete = null;
    this.fileDeleteError = null;
  }

  confirmFileDelete(): void {
    if (!this.fileToDelete) return;
    this.deletingFile = true;
    this.fileDeleteError = null;

    this.cloudServicesService.deleteS3File(this.fileToDelete.id).subscribe({
      next: () => {
        this.deletingFile = false;
        this.files = this.files.filter(f => f.id !== this.fileToDelete!.id);
        this.closeFileDeleteConfirm();
        this.showToast('File deleted successfully', 'success');
      },
      error: (err) => {
        this.deletingFile = false;
        this.fileDeleteError = err?.error?.message || err?.message || 'Failed to delete file. Please try again.';
      }
    });
  }

  // ── Toast ──

  showToast(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    setTimeout(() => { this.toastMessage = null; }, 4000);
  }
}
