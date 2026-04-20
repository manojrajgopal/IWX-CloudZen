import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  StartConnectionRequest,
  StartConnectionResponse,
  ExecuteCommandRequest,
  ExecuteCommandResponse,
  DisconnectResponse,
  SessionStatusResponse,
  ActiveSessionsListResponse,
  FileListResponse,
  FileReadResponse,
  FileOperationResponse,
  FileWriteRequest,
  FileDeleteRequest,
  FileMkdirRequest,
  FileRenameRequest,
  FileCopyRequest,
  FileDownloadResponse,
  FileSearchRequest,
  FileSearchResponse,
  SystemInfoResponse,
  ManualConnectionRequest
} from '../models/ec2-connection.model';

@Injectable({ providedIn: 'root' })
export class Ec2ConnectionService {
  private readonly base = `${environment.apiUrl}/api/cloud/services/ec2-connection`;

  constructor(private http: HttpClient) {}

  // ── Session ────────────────────────────────────────────────────────────────

  connect(accountId: number, request: StartConnectionRequest): Observable<StartConnectionResponse> {
    return this.http.post<StartConnectionResponse>(
      `${this.base}/aws/connect?accountId=${accountId}`, request);
  }

  connectManual(accountId: number, request: ManualConnectionRequest): Observable<StartConnectionResponse> {
    return this.http.post<StartConnectionResponse>(
      `${this.base}/aws/connect-manual?accountId=${accountId}`, request);
  }

  execute(accountId: number, request: ExecuteCommandRequest): Observable<ExecuteCommandResponse> {
    return this.http.post<ExecuteCommandResponse>(
      `${this.base}/aws/execute?accountId=${accountId}`, request);
  }

  tabComplete(accountId: number, sessionId: string, partial: string): Observable<{ completions: string[] }> {
    const params = `accountId=${accountId}&sessionId=${encodeURIComponent(sessionId)}&partial=${encodeURIComponent(partial)}`;
    return this.http.get<{ completions: string[] }>(`${this.base}/aws/tab-complete?${params}`);
  }

  disconnect(accountId: number, sessionId: string): Observable<DisconnectResponse> {
    return this.http.post<DisconnectResponse>(
      `${this.base}/aws/disconnect?accountId=${accountId}`, { sessionId });
  }

  getSessionStatus(accountId: number, sessionId: string): Observable<SessionStatusResponse> {
    return this.http.get<SessionStatusResponse>(
      `${this.base}/aws/session/${sessionId}?accountId=${accountId}`);
  }

  listActiveSessions(accountId: number): Observable<ActiveSessionsListResponse> {
    return this.http.get<ActiveSessionsListResponse>(
      `${this.base}/aws/sessions?accountId=${accountId}`);
  }

  // ── System Info ────────────────────────────────────────────────────────────

  getSystemInfo(accountId: number, sessionId: string): Observable<SystemInfoResponse> {
    return this.http.get<SystemInfoResponse>(
      `${this.base}/aws/system-info?accountId=${accountId}&sessionId=${sessionId}`);
  }

  // ── File Browser ───────────────────────────────────────────────────────────

  listDirectory(accountId: number, sessionId: string, path: string): Observable<FileListResponse> {
    return this.http.get<FileListResponse>(
      `${this.base}/aws/files/list?accountId=${accountId}&sessionId=${encodeURIComponent(sessionId)}&path=${encodeURIComponent(path)}`);
  }

  readFile(accountId: number, sessionId: string, path: string): Observable<FileReadResponse> {
    return this.http.get<FileReadResponse>(
      `${this.base}/aws/files/read?accountId=${accountId}&sessionId=${encodeURIComponent(sessionId)}&path=${encodeURIComponent(path)}`);
  }

  writeFile(accountId: number, request: FileWriteRequest): Observable<FileOperationResponse> {
    return this.http.post<FileOperationResponse>(
      `${this.base}/aws/files/write?accountId=${accountId}`, request);
  }

  deleteFile(accountId: number, request: FileDeleteRequest): Observable<FileOperationResponse> {
    return this.http.delete<FileOperationResponse>(
      `${this.base}/aws/files/delete?accountId=${accountId}`, { body: request });
  }

  makeDirectory(accountId: number, request: FileMkdirRequest): Observable<FileOperationResponse> {
    return this.http.post<FileOperationResponse>(
      `${this.base}/aws/files/mkdir?accountId=${accountId}`, request);
  }

  renameOrMove(accountId: number, request: FileRenameRequest): Observable<FileOperationResponse> {
    return this.http.post<FileOperationResponse>(
      `${this.base}/aws/files/rename?accountId=${accountId}`, request);
  }

  copyFile(accountId: number, request: FileCopyRequest): Observable<FileOperationResponse> {
    return this.http.post<FileOperationResponse>(
      `${this.base}/aws/files/copy?accountId=${accountId}`, request);
  }

  downloadFile(accountId: number, sessionId: string, path: string): Observable<FileDownloadResponse> {
    return this.http.get<FileDownloadResponse>(
      `${this.base}/aws/files/download?accountId=${accountId}&sessionId=${encodeURIComponent(sessionId)}&path=${encodeURIComponent(path)}`);
  }

  searchFiles(accountId: number, request: FileSearchRequest): Observable<FileSearchResponse> {
    return this.http.post<FileSearchResponse>(
      `${this.base}/aws/files/search?accountId=${accountId}`, request);
  }
}
