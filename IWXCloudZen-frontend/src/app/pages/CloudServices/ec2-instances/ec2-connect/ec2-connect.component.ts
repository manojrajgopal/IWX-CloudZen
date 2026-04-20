import {
  Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { CloudAccountService } from '../../../../services/cloud-account.service';
import { CloudServicesService } from '../../../../services/cloud-services.service';
import { Ec2ConnectionService } from '../../../../services/ec2-connection.service';
import { CloudAccount } from '../../../../models/cloud-account.model';
import { Ec2Instance } from '../../../../models/cloud-services.model';
import {
  StartConnectionResponse,
  ExecuteCommandResponse,
  FileListResponse,
  FileEntryInfo,
  FileReadResponse,
  SystemInfoResponse
} from '../../../../models/ec2-connection.model';

// ── Local types ──────────────────────────────────────────────────────────────

type ViewMode = 'terminal' | 'explorer';
type ConnectionMethod = 'SSM' | 'SSH';

interface TerminalLine {
  type: 'input' | 'output' | 'error' | 'system' | 'banner';
  text: string;
  exitCode?: number;
  timestamp?: Date;
}

interface ContextMenuItem {
  label: string;
  icon: string;
  action: () => void;
  danger?: boolean;
  divider?: boolean;
}

@Component({
  selector: 'app-ec2-connect',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ec2-connect.component.html',
  styleUrls: ['./ec2-connect.component.css']
})
export class Ec2ConnectComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('terminalOutput') private terminalOutput!: ElementRef<HTMLDivElement>;
  @ViewChild('commandInput') private commandInput!: ElementRef<HTMLInputElement>;
  @ViewChild('fileEditorTextarea') private fileEditorTextarea!: ElementRef<HTMLTextAreaElement>;

  // ── Loading / Connection State ──

  /** Phase: 'init' → 'booting' → 'connected' | 'error' */
  phase: 'init' | 'booting' | 'connected' | 'error' = 'init';
  bootMessages: string[] = [];
  bootProgress = 0;
  bootError: string | null = null;
  private bootInterval: any;
  private shouldScrollTerminal = false;

  // ── Instance / Account ──

  instance: Ec2Instance | null = null;
  account: CloudAccount | null = null;
  accounts: CloudAccount[] = [];
  loadingInstance = true;

  // ── Session ──

  session: StartConnectionResponse | null = null;
  connectionMethod: ConnectionMethod = 'SSH';
  systemInfo: SystemInfoResponse | null = null;

  // ── View mode ──

  viewMode: ViewMode = 'terminal';

  // ── Terminal ──

  terminalLines: TerminalLine[] = [];
  currentCommand = '';
  commandHistory: string[] = [];
  historyIndex = -1;
  currentWorkingDir = '';
  isExecuting = false;

  // ── File Explorer ──

  explorerPath = '/';
  explorerLoading = false;
  explorerEntries: FileEntryInfo[] = [];
  explorerError: string | null = null;
  selectedEntry: FileEntryInfo | null = null;
  explorerBreadcrumbs: string[] = [];

  // Context menu
  showContextMenu = false;
  contextMenuX = 0;
  contextMenuY = 0;
  contextMenuItems: ContextMenuItem[] = [];

  // File editor
  showFileEditor = false;
  editorPath = '';
  editorContent = '';
  editorReadOnly = false;
  editorLoading = false;
  editorSaving = false;
  editorError: string | null = null;
  editorMessage: string | null = null;
  editorLanguage = '';

  // New file / folder dialog
  showNewDialog = false;
  newDialogType: 'file' | 'folder' = 'file';
  newItemName = '';
  newItemError: string | null = null;

  // Rename dialog
  showRenameDialog = false;
  renameTarget: FileEntryInfo | null = null;
  renameNewName = '';
  renameError: string | null = null;

  // Delete confirm dialog
  showDeleteDialog = false;
  deleteTarget: FileEntryInfo | null = null;
  deleteLoading = false;

  // Search
  showSearchPanel = false;
  searchQuery = '';
  searchResults: FileEntryInfo[] = [];
  searchLoading = false;

  // Toast
  toastMessage: string | null = null;
  toastType: 'success' | 'error' | 'info' = 'info';
  private toastTimeout: any;

  // ── Connection dialog ──

  showConnectionDialog = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cloudAccountService: CloudAccountService,
    private cloudServicesService: CloudServicesService,
    private ec2ConnectionService: Ec2ConnectionService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id || isNaN(id)) {
      this.phase = 'error';
      this.bootError = 'Invalid EC2 Instance ID';
      this.loadingInstance = false;
      return;
    }
    this.loadInstance(id);
  }

  ngOnDestroy(): void {
    clearInterval(this.bootInterval);
    clearTimeout(this.toastTimeout);
    // Disconnect session on destroy
    if (this.session && this.account) {
      this.ec2ConnectionService.disconnect(this.account.id, this.session.sessionId).subscribe();
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollTerminal) {
      this.scrollTerminalToBottom();
      this.shouldScrollTerminal = false;
    }
  }

  // ── Instance Loading ─────────────────────────────────────────────────────

  private loadInstance(instanceId: number): void {
    this.cloudAccountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        const requests = accounts.map(a =>
          this.cloudServicesService.getEc2Instances(a.id).pipe(catchError(() => of({ instances: [] })))
        );
        forkJoin(requests).subscribe({
          next: (results: any[]) => {
            const all: Ec2Instance[] = results.flatMap((r: any) => r.instances || []);
            this.instance = all.find(i => i.id === instanceId) || null;
            if (this.instance) {
              this.account = accounts.find(a => a.id === this.instance!.cloudAccountId) || null;
              this.loadingInstance = false;
              // Show method selection screen — do NOT auto-connect
            } else {
              this.phase = 'error';
              this.bootError = 'EC2 Instance not found.';
              this.loadingInstance = false;
            }
          },
          error: () => {
            this.phase = 'error';
            this.bootError = 'Failed to load EC2 data.';
            this.loadingInstance = false;
          }
        });
      },
      error: () => {
        this.phase = 'error';
        this.bootError = 'Failed to load cloud accounts.';
        this.loadingInstance = false;
      }
    });
  }

  // ── Boot Sequence ─────────────────────────────────────────────────────────

  startBootSequence(): void {
    if (!this.instance) return;

    if (this.instance.state !== 'running') {
      this.phase = 'error';
      this.bootError = `Instance is "${this.instance.state}". It must be running to connect.`;
      return;
    }

    this.phase = 'booting';
    this.bootMessages = [];
    this.bootProgress = 0;

    const msgs = [
      `Initializing connection to ${this.instance.name || this.instance.instanceId}...`,
      `Resolving instance metadata...`,
      `Connecting via ${this.connectionMethod}...`,
      `Authenticating...`,
      `Establishing secure channel...`,
      `Loading system information...`,
    ];

    let step = 0;
    this.bootInterval = setInterval(() => {
      if (step < msgs.length) {
        this.bootMessages.push(msgs[step]);
        this.bootProgress = Math.round(((step + 1) / (msgs.length + 1)) * 90);
        step++;
      }
    }, 400);

    // Actually connect after 1.2 seconds
    setTimeout(() => this.doConnect(), 1200);
  }

  /** Called from the init-screen "Connect" button. */
  connectWithOptions(): void {
    this.startBootSequence();
  }

  private doConnect(): void {
    if (!this.instance || !this.account) return;

    this.ec2ConnectionService.connect(this.account.id, {
      instanceDbId: this.instance.id,
      connectionMethod: this.connectionMethod
    }).subscribe({
      next: (resp) => {
        this.session = resp;
        this.bootProgress = 95;
        this.bootMessages.push(`Connected! Session ID: ${resp.sessionId.substring(0, 8)}...`);

        // Load system info
        this.ec2ConnectionService.getSystemInfo(this.account!.id, resp.sessionId).subscribe({
          next: (info) => {
            this.systemInfo = info;
            this.currentWorkingDir = info.homeDirectory || '/home/' + resp.osUser;
            clearInterval(this.bootInterval);
            this.bootProgress = 100;
            this.bootMessages.push('System ready.');
            setTimeout(() => {
              this.phase = 'connected';
              this.initTerminal();
              if (this.viewMode === 'explorer') {
                this.navigateExplorer(this.currentWorkingDir || '/');
              }
            }, 600);
          },
          error: () => {
            // Proceed without system info
            clearInterval(this.bootInterval);
            this.bootProgress = 100;
            this.bootMessages.push('Connected (system info unavailable).');
            setTimeout(() => {
              this.phase = 'connected';
              this.initTerminal();
            }, 400);
          }
        });
      },
      error: (err) => {
        clearInterval(this.bootInterval);
        this.phase = 'error';
        this.bootError = err?.error?.message || err?.message || 'Connection failed.';
      }
    });
  }

  retry(): void {
    this.phase = 'init';
    this.bootMessages = [];
    this.bootProgress = 0;
    this.bootError = null;
    this.session = null;
    this.terminalLines = [];
    if (this.instance) this.startBootSequence();
  }

  // ── Terminal ─────────────────────────────────────────────────────────────

  private initTerminal(): void {
    const info = this.systemInfo;
    const inst = this.instance!;
    const sess = this.session!;

    this.terminalLines = [
      { type: 'banner', text: '╔══════════════════════════════════════════════════════════╗' },
      { type: 'banner', text: `║   IWX CloudZen — EC2 Terminal                            ║` },
      { type: 'banner', text: '╚══════════════════════════════════════════════════════════╝' },
      { type: 'system', text: '' },
      { type: 'system', text: `  Instance : ${inst.name || inst.instanceId}` },
      { type: 'system', text: `  ID       : ${inst.instanceId}` },
      { type: 'system', text: `  Type     : ${inst.instanceType}` },
      { type: 'system', text: `  IP       : ${inst.publicIpAddress || inst.privateIpAddress}` },
      { type: 'system', text: `  Method   : ${sess.connectionMethod}` },
      { type: 'system', text: `  User     : ${sess.osUser}` },
      ...(info ? [
        { type: 'system' as const, text: `  OS       : ${info.kernel}` },
        { type: 'system' as const, text: `  Hostname : ${info.hostname}` },
        { type: 'system' as const, text: `  CPU      : ${info.cpu}` },
        { type: 'system' as const, text: `  Memory   : ${info.memory}` },
        { type: 'system' as const, text: `  Disk     : ${info.diskUsage}` },
      ] : []),
      { type: 'system', text: '' },
      { type: 'system', text: '  Type "help" for available shortcuts.' },
      { type: 'system', text: '' },
    ];

    this.shouldScrollTerminal = true;
    setTimeout(() => this.focusCommandInput(), 100);
  }

  getPrompt(): string {
    if (!this.session) return '$ ';
    const host = this.systemInfo?.hostname || this.instance?.instanceId || 'ec2';
    const user = this.session.osUser;
    const dir = this.currentWorkingDir || '~';
    const homeDir = '/home/' + user;
    let displayDir: string;
    if (dir === homeDir) {
      displayDir = '~';
    } else if (dir.startsWith(homeDir + '/')) {
      displayDir = '~' + dir.substring(homeDir.length);
    } else {
      displayDir = dir;
    }
    return `${user}@${host}:${displayDir}$ `;
  }

  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.executeCurrentCommand();
    } else if (event.key === 'Tab') {
      event.preventDefault();
      this.doTabComplete();
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.navigateHistory(-1);
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.navigateHistory(1);
    } else if (event.key === 'l' && event.ctrlKey) {
      event.preventDefault();
      this.clearTerminal();
    } else if (event.key === 'c' && event.ctrlKey) {
      event.preventDefault();
      this.terminalLines.push({ type: 'input', text: this.getPrompt() + this.currentCommand + '^C' });
      this.currentCommand = '';
      this.shouldScrollTerminal = true;
    }
  }

  private doTabComplete(): void {
    if (!this.session || !this.account || this.isExecuting) return;

    const cmd = this.currentCommand;
    // Split on last space to get the partial word being typed
    const lastSpace = cmd.lastIndexOf(' ');
    const partial = lastSpace >= 0 ? cmd.substring(lastSpace + 1) : cmd;
    const cmdPrefix = lastSpace >= 0 ? cmd.substring(0, lastSpace + 1) : '';

    this.ec2ConnectionService.tabComplete(this.account.id, this.session.sessionId, partial).subscribe({
      next: (resp) => {
        const completions = resp.completions;
        if (completions.length === 0) return;

        if (completions.length === 1) {
          // Single match — auto-fill
          this.currentCommand = cmdPrefix + completions[0];
          this.cdr.detectChanges();
        } else {
          // Multiple matches — show them as output lines (bash-style)
          completions.forEach(c => this.terminalLines.push({ type: 'output', text: c }));
          this.shouldScrollTerminal = true;
          this.cdr.detectChanges();
        }
      },
      error: () => {} // silently ignore
    });
  }

  private navigateHistory(direction: number): void {
    const cmdHistory = this.commandHistory.filter(c => c.trim());
    if (cmdHistory.length === 0) return;

    if (direction === -1) {
      this.historyIndex = Math.min(this.historyIndex + 1, cmdHistory.length - 1);
    } else {
      this.historyIndex = Math.max(this.historyIndex - 1, -1);
    }

    this.currentCommand = this.historyIndex >= 0
      ? cmdHistory[cmdHistory.length - 1 - this.historyIndex]
      : '';
  }

  private executeCurrentCommand(): void {
    const cmd = this.currentCommand.trim();
    this.currentCommand = '';
    this.historyIndex = -1;

    if (!cmd) {
      this.terminalLines.push({ type: 'input', text: this.getPrompt() });
      this.shouldScrollTerminal = true;
      return;
    }

    this.terminalLines.push({ type: 'input', text: this.getPrompt() + cmd });
    this.commandHistory.push(cmd);
    this.shouldScrollTerminal = true;

    // Built-in commands
    if (cmd === 'clear' || cmd === 'cls') {
      this.terminalLines = [];
      return;
    }

    if (cmd === 'help') {
      this.printHelp();
      return;
    }

    if (cmd === 'explorer') {
      this.switchView('explorer');
      this.terminalLines.push({ type: 'system', text: 'Switching to File Explorer...' });
      return;
    }

    if (cmd === 'exit') {
      this.handleDisconnect();
      return;
    }

    if (!this.session || !this.account) return;

    this.isExecuting = true;

    this.ec2ConnectionService.execute(this.account.id, {
      sessionId: this.session.sessionId,
      command: cmd
    }).subscribe({
      next: (resp: ExecuteCommandResponse) => {
        // Update working directory from backend response
        if (resp.workingDirectory) {
          this.currentWorkingDir = resp.workingDirectory;
        }

        if (resp.standardOutput.trim()) {
          resp.standardOutput.split('\n').forEach(line => {
            this.terminalLines.push({ type: 'output', text: line, exitCode: resp.exitCode });
          });
        }
        if (resp.standardError.trim()) {
          resp.standardError.split('\n').forEach(line => {
            this.terminalLines.push({ type: 'error', text: line });
          });
        }

        this.isExecuting = false;
        this.shouldScrollTerminal = true;
        this.cdr.detectChanges();
        setTimeout(() => this.focusCommandInput(), 50);
      },
      error: (err) => {
        const msg = err?.error?.message || 'Command execution failed.';
        this.terminalLines.push({ type: 'error', text: msg });
        this.isExecuting = false;
        this.shouldScrollTerminal = true;
        this.cdr.detectChanges();
        setTimeout(() => this.focusCommandInput(), 50);
      }
    });
  }

  private printHelp(): void {
    const lines = [
      '',
      '  Keyboard shortcuts:',
      '    ↑/↓         Navigate command history',
      '    Ctrl+L      Clear terminal',
      '    Ctrl+C      Interrupt current input',
      '',
      '  Built-in commands:',
      '    clear       Clear the screen',
      '    explorer    Switch to File Explorer mode',
      '    exit        Disconnect from instance',
      '    help        Show this help',
      '',
      '  All other commands are sent to the EC2 instance.',
      '',
    ];
    lines.forEach(l => this.terminalLines.push({ type: 'system', text: l }));
    this.shouldScrollTerminal = true;
  }

  clearTerminal(): void {
    this.terminalLines = [];
  }

  focusCommandInput(): void {
    try { this.commandInput?.nativeElement?.focus(); } catch { /* ignore */ }
  }

  private scrollTerminalToBottom(): void {
    try {
      const el = this.terminalOutput?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch { /* ignore */ }
  }

  // ── File Explorer ─────────────────────────────────────────────────────────

  navigateExplorer(path: string): void {
    if (!this.session || !this.account) return;
    this.explorerLoading = true;
    this.explorerError = null;
    this.selectedEntry = null;

    this.ec2ConnectionService.listDirectory(this.account.id, this.session.sessionId, path).subscribe({
      next: (resp: FileListResponse) => {
        this.explorerPath = resp.currentPath;
        this.explorerEntries = resp.entries;
        this.buildBreadcrumbs(resp.currentPath);
        this.explorerLoading = false;
      },
      error: (err) => {
        this.explorerError = err?.error?.message || 'Failed to list directory.';
        this.explorerLoading = false;
      }
    });
  }

  private buildBreadcrumbs(path: string): void {
    const parts = path.split('/').filter(p => p);
    this.explorerBreadcrumbs = ['/', ...parts.map((_, i) => '/' + parts.slice(0, i + 1).join('/'))];
  }

  getBreadcrumbLabel(crumb: string): string {
    if (crumb === '/') return '/';
    return crumb.split('/').filter(p => p).pop() || crumb;
  }

  onEntryClick(entry: FileEntryInfo): void {
    this.selectedEntry = entry;
  }

  onEntryDoubleClick(entry: FileEntryInfo): void {
    if (entry.isDirectory) {
      this.navigateExplorer(entry.fullPath);
    } else {
      this.openFileEditor(entry);
    }
  }

  openFileEditor(entry: FileEntryInfo): void {
    if (!this.session || !this.account) return;
    this.editorPath = entry.fullPath;
    this.editorLoading = true;
    this.editorError = null;
    this.editorMessage = null;
    this.editorReadOnly = false;
    this.editorLanguage = this.getEditorLanguage(entry.extension);
    this.showFileEditor = true;

    this.ec2ConnectionService.readFile(this.account.id, this.session.sessionId, entry.fullPath).subscribe({
      next: (resp: FileReadResponse) => {
        this.editorContent = resp.content;
        this.editorReadOnly = resp.isBinary;
        this.editorLoading = false;
        setTimeout(() => this.fileEditorTextarea?.nativeElement?.focus(), 100);
      },
      error: (err) => {
        this.editorError = err?.error?.message || 'Failed to read file.';
        this.editorLoading = false;
      }
    });
  }

  saveFile(): void {
    if (!this.session || !this.account || this.editorReadOnly) return;
    this.editorSaving = true;
    this.editorMessage = null;
    this.editorError = null;

    this.ec2ConnectionService.writeFile(this.account.id, {
      sessionId: this.session.sessionId,
      path: this.editorPath,
      content: this.editorContent
    }).subscribe({
      next: () => {
        this.editorMessage = 'File saved successfully.';
        this.editorSaving = false;
      },
      error: (err) => {
        this.editorError = err?.error?.message || 'Failed to save file.';
        this.editorSaving = false;
      }
    });
  }

  closeFileEditor(): void {
    this.showFileEditor = false;
    this.editorContent = '';
    this.editorPath = '';
    this.editorError = null;
    this.editorMessage = null;
  }

  private getEditorLanguage(ext: string): string {
    const map: Record<string, string> = {
      '.js': 'javascript', '.ts': 'typescript', '.py': 'python', '.sh': 'shell',
      '.bash': 'shell', '.zsh': 'shell', '.json': 'json', '.xml': 'xml',
      '.yml': 'yaml', '.yaml': 'yaml', '.html': 'html', '.css': 'css',
      '.md': 'markdown', '.txt': 'text', '.conf': 'conf', '.cfg': 'conf',
      '.log': 'log', '.env': 'env', '.sql': 'sql', '.go': 'go',
      '.java': 'java', '.c': 'c', '.cpp': 'cpp', '.rs': 'rust',
    };
    return map[ext?.toLowerCase()] || 'text';
  }

  // ── Context Menu ──────────────────────────────────────────────────────────

  onRightClick(event: MouseEvent, entry: FileEntryInfo): void {
    event.preventDefault();
    this.selectedEntry = entry;
    this.contextMenuX = event.clientX;
    this.contextMenuY = event.clientY;

    this.contextMenuItems = [
      ...(entry.isDirectory ? [
        { label: 'Open', icon: 'folder-open', action: () => { this.navigateExplorer(entry.fullPath); this.closeContextMenu(); } },
        { label: 'Open in Terminal', icon: 'terminal', action: () => { this.openInTerminal(entry.fullPath); this.closeContextMenu(); } },
      ] : [
        { label: 'Open', icon: 'document-text', action: () => { this.openFileEditor(entry); this.closeContextMenu(); } },
        { label: 'Open in Terminal (cat)', icon: 'terminal', action: () => { this.catInTerminal(entry.fullPath); this.closeContextMenu(); } },
        { label: 'Download', icon: 'arrow-down-tray', action: () => { this.downloadEntry(entry); this.closeContextMenu(); } },
      ]),
      { label: '', icon: '', action: () => {}, divider: true },
      { label: 'Rename', icon: 'pencil', action: () => { this.openRenameDialog(entry); this.closeContextMenu(); } },
      { label: 'Copy Path', icon: 'clipboard', action: () => { this.copyPath(entry.fullPath); this.closeContextMenu(); } },
      { label: '', icon: '', action: () => {}, divider: true },
      { label: 'Delete', icon: 'trash', action: () => { this.openDeleteDialog(entry); this.closeContextMenu(); }, danger: true },
    ];

    this.showContextMenu = true;
  }

  closeContextMenu(): void {
    this.showContextMenu = false;
  }

  // ── File Operations ───────────────────────────────────────────────────────

  openInTerminal(path: string): void {
    this.switchView('terminal');
    this.currentCommand = `cd "${path}"`;
    setTimeout(() => this.executeCurrentCommand(), 100);
  }

  catInTerminal(path: string): void {
    this.switchView('terminal');
    this.currentCommand = `cat "${path}"`;
    setTimeout(() => this.executeCurrentCommand(), 100);
  }

  downloadEntry(entry: FileEntryInfo): void {
    if (!this.session || !this.account || entry.isDirectory) return;

    this.ec2ConnectionService.downloadFile(this.account.id, this.session.sessionId, entry.fullPath).subscribe({
      next: (resp) => {
        const binary = atob(resp.contentBase64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
        const blob = new Blob([bytes], { type: resp.mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = resp.fileName;
        a.click();
        URL.revokeObjectURL(url);
        this.showToast(`Downloading "${resp.fileName}"`, 'success');
      },
      error: (err) => {
        this.showToast(err?.error?.message || 'Download failed.', 'error');
      }
    });
  }

  copyPath(path: string): void {
    navigator.clipboard.writeText(path).then(() => this.showToast('Path copied!', 'success'));
  }

  openNewDialog(type: 'file' | 'folder'): void {
    this.newDialogType = type;
    this.newItemName = '';
    this.newItemError = null;
    this.showNewDialog = true;
  }

  confirmNewItem(): void {
    if (!this.newItemName.trim() || !this.session || !this.account) return;
    const path = this.explorerPath.replace(/\/$/, '') + '/' + this.newItemName.trim();

    if (this.newDialogType === 'folder') {
      this.ec2ConnectionService.makeDirectory(this.account.id, {
        sessionId: this.session.sessionId,
        path,
        createParents: true
      }).subscribe({
        next: () => {
          this.showNewDialog = false;
          this.showToast('Folder created!', 'success');
          this.navigateExplorer(this.explorerPath);
        },
        error: (err) => { this.newItemError = err?.error?.message || 'Failed to create folder.'; }
      });
    } else {
      this.ec2ConnectionService.writeFile(this.account.id, {
        sessionId: this.session.sessionId,
        path,
        content: ''
      }).subscribe({
        next: () => {
          this.showNewDialog = false;
          this.showToast('File created!', 'success');
          this.navigateExplorer(this.explorerPath);
        },
        error: (err) => { this.newItemError = err?.error?.message || 'Failed to create file.'; }
      });
    }
  }

  openRenameDialog(entry: FileEntryInfo): void {
    this.renameTarget = entry;
    this.renameNewName = entry.name;
    this.renameError = null;
    this.showRenameDialog = true;
  }

  confirmRename(): void {
    if (!this.renameNewName.trim() || !this.renameTarget || !this.session || !this.account) return;
    const dir = this.explorerPath.replace(/\/$/, '');
    const newPath = dir + '/' + this.renameNewName.trim();

    this.ec2ConnectionService.renameOrMove(this.account.id, {
      sessionId: this.session.sessionId,
      oldPath: this.renameTarget.fullPath,
      newPath
    }).subscribe({
      next: () => {
        this.showRenameDialog = false;
        this.showToast('Renamed successfully!', 'success');
        this.navigateExplorer(this.explorerPath);
      },
      error: (err) => { this.renameError = err?.error?.message || 'Rename failed.'; }
    });
  }

  openDeleteDialog(entry: FileEntryInfo): void {
    this.deleteTarget = entry;
    this.showDeleteDialog = true;
  }

  confirmDelete(): void {
    if (!this.deleteTarget || !this.session || !this.account) return;
    this.deleteLoading = true;

    this.ec2ConnectionService.deleteFile(this.account.id, {
      sessionId: this.session.sessionId,
      path: this.deleteTarget.fullPath,
      recursive: this.deleteTarget.isDirectory
    }).subscribe({
      next: () => {
        this.showDeleteDialog = false;
        this.deleteLoading = false;
        this.deleteTarget = null;
        this.showToast('Deleted successfully!', 'success');
        this.navigateExplorer(this.explorerPath);
      },
      error: (err) => {
        this.deleteLoading = false;
        this.showToast(err?.error?.message || 'Delete failed.', 'error');
        this.showDeleteDialog = false;
      }
    });
  }

  searchFiles(): void {
    if (!this.searchQuery.trim() || !this.session || !this.account) return;
    this.searchLoading = true;
    this.searchResults = [];

    this.ec2ConnectionService.searchFiles(this.account.id, {
      sessionId: this.session.sessionId,
      directory: this.explorerPath,
      pattern: this.searchQuery.trim(),
      maxResults: 100
    }).subscribe({
      next: (resp) => {
        this.searchResults = resp.results;
        this.searchLoading = false;
      },
      error: () => { this.searchLoading = false; }
    });
  }

  // ── View switching ────────────────────────────────────────────────────────

  switchView(mode: ViewMode): void {
    this.viewMode = mode;
    if (mode === 'explorer' && this.phase === 'connected' && !this.explorerEntries.length) {
      this.navigateExplorer(this.currentWorkingDir || '/');
    }
    if (mode === 'terminal') {
      setTimeout(() => this.focusCommandInput(), 100);
    }
  }

  // ── Disconnect ────────────────────────────────────────────────────────────

  handleDisconnect(): void {
    if (!this.session || !this.account) {
      this.router.navigate(['/dashboard/ec2-instances', this.instance?.id]);
      return;
    }

    this.ec2ConnectionService.disconnect(this.account.id, this.session.sessionId).subscribe({
      next: () => {
        this.session = null;
        this.router.navigate(['/dashboard/ec2-instances', this.instance?.id]);
      },
      error: () => {
        this.session = null;
        this.router.navigate(['/dashboard/ec2-instances', this.instance?.id]);
      }
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  getFileIcon(entry: FileEntryInfo): string {
    if (entry.isDirectory) return 'folder';
    if (entry.isSymlink) return 'link';
    const ext = (entry.extension || '').toLowerCase();
    if (['.jpg', '.jpeg', '.png', '.gif', '.svg', '.webp', '.bmp'].includes(ext)) return 'photo';
    if (['.mp4', '.avi', '.mov', '.mkv', '.webm'].includes(ext)) return 'film';
    if (['.mp3', '.wav', '.ogg', '.flac'].includes(ext)) return 'musical-note';
    if (['.zip', '.tar', '.gz', '.bz2', '.xz', '.rar'].includes(ext)) return 'archive-box';
    if (['.pdf'].includes(ext)) return 'document-pdf';
    if (['.js', '.ts', '.py', '.go', '.java', '.c', '.cpp', '.rs', '.sh', '.bash'].includes(ext)) return 'code';
    if (['.json', '.yml', '.yaml', '.xml', '.conf', '.cfg', '.env', '.ini', '.toml'].includes(ext)) return 'cog';
    if (['.txt', '.md', '.log', '.readme'].includes(ext)) return 'document-text';
    return 'document';
  }

  getFileIconColor(entry: FileEntryInfo): string {
    if (entry.isDirectory) return 'text-yellow-500';
    const ext = (entry.extension || '').toLowerCase();
    if (['.jpg', '.jpeg', '.png', '.gif', '.svg', '.webp'].includes(ext)) return 'text-purple-500';
    if (['.js', '.ts'].includes(ext)) return 'text-yellow-400';
    if (['.py'].includes(ext)) return 'text-blue-400';
    if (['.sh', '.bash', '.zsh'].includes(ext)) return 'text-green-400';
    if (['.json', '.yml', '.yaml', '.xml'].includes(ext)) return 'text-orange-400';
    if (['.txt', '.md'].includes(ext)) return 'text-gray-400';
    if (['.log'].includes(ext)) return 'text-red-400';
    if (['.zip', '.tar', '.gz'].includes(ext)) return 'text-amber-500';
    return 'text-gray-500';
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '—';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    if (bytes < 1024 * 1024 * 1024) return (bytes / 1024 / 1024).toFixed(1) + ' MB';
    return (bytes / 1024 / 1024 / 1024).toFixed(2) + ' GB';
  }

  formatModified(dateStr: string): string {
    if (!dateStr) return '—';
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }).format(d);
    } catch { return dateStr; }
  }

  getLineClass(line: TerminalLine): string {
    switch (line.type) {
      case 'input': return 'terminal-input-line';
      case 'output': return 'terminal-output-line';
      case 'error': return 'terminal-error-line';
      case 'system': return 'terminal-system-line';
      case 'banner': return 'terminal-banner-line';
      default: return '';
    }
  }

  showToast(msg: string, type: 'success' | 'error' | 'info'): void {
    clearTimeout(this.toastTimeout);
    this.toastMessage = msg;
    this.toastType = type;
    this.toastTimeout = setTimeout(() => { this.toastMessage = null; }, 3500);
  }

  navigateBack(): void {
    this.router.navigate(['/dashboard/ec2-instances', this.instance?.id]);
  }
}
