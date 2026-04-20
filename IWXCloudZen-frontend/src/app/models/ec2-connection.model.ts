// ── Session Models ───────────────────────────────────────────────────────────

export interface StartConnectionRequest {
  instanceDbId: number;
  osUser?: string;
  connectionMethod?: 'SSM' | 'SSH';
  privateKeyContent?: string;
}

export interface StartConnectionResponse {
  sessionId: string;
  status: string;
  connectionMethod: string;
  instanceId: string;
  ipAddress: string;
  osUser: string;
  connectedAt: string;
}

export interface ExecuteCommandRequest {
  sessionId: string;
  command: string;
}

export interface ExecuteCommandResponse {
  sessionId: string;
  command: string;
  standardOutput: string;
  standardError: string;
  exitCode: number;
  workingDirectory: string;
  executedAt: string;
}

export interface DisconnectResponse {
  sessionId: string;
  status: string;
  disconnectedAt: string;
}

export interface CommandLogEntry {
  command: string;
  standardOutput: string;
  standardError: string;
  exitCode: number;
  executedAt: string;
}

export interface SessionStatusResponse {
  sessionId: string;
  status: string;
  connectionMethod: string;
  instanceId: string;
  ipAddress: string;
  osUser: string;
  connectedAt: string;
  commandHistory: CommandLogEntry[];
}

export interface ActiveSessionInfo {
  sessionId: string;
  connectionMethod: string;
  instanceId: string;
  ipAddress: string;
  osUser: string;
  status: string;
  commandCount: number;
  connectedAt: string;
}

export interface ActiveSessionsListResponse {
  sessions: ActiveSessionInfo[];
}

// ── File Browser Models ───────────────────────────────────────────────────────

export interface FileEntryInfo {
  name: string;
  fullPath: string;
  isDirectory: boolean;
  isSymlink: boolean;
  size: number;
  permissions: string;
  owner: string;
  group: string;
  modifiedAt: string;
  extension: string;
  linkTarget: string;
}

export interface FileListResponse {
  currentPath: string;
  parentPath: string;
  entries: FileEntryInfo[];
}

export interface FileReadResponse {
  path: string;
  content: string;
  isBinary: boolean;
  size: number;
  encoding: string;
}

export interface FileOperationResponse {
  success: boolean;
  message: string;
}

export interface FileWriteRequest {
  sessionId: string;
  path: string;
  content: string;
  append?: boolean;
}

export interface FileDeleteRequest {
  sessionId: string;
  path: string;
  recursive?: boolean;
}

export interface FileMkdirRequest {
  sessionId: string;
  path: string;
  createParents?: boolean;
}

export interface FileRenameRequest {
  sessionId: string;
  oldPath: string;
  newPath: string;
}

export interface FileCopyRequest {
  sessionId: string;
  sourcePath: string;
  destinationPath: string;
}

export interface FileDownloadResponse {
  path: string;
  fileName: string;
  contentBase64: string;
  size: number;
  mimeType: string;
}

export interface FileSearchRequest {
  sessionId: string;
  directory: string;
  pattern: string;
  caseSensitive?: boolean;
  maxResults?: number;
}

export interface FileSearchResponse {
  directory: string;
  pattern: string;
  results: FileEntryInfo[];
  totalFound: number;
}

export interface SystemInfoResponse {
  sessionId: string;
  hostname: string;
  osRelease: string;
  kernel: string;
  uptime: string;
  cpu: string;
  memory: string;
  diskUsage: string;
  homeDirectory: string;
  remoteUser: string;
  shell: string;
  workingDirectory: string;
}
