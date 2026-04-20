namespace IWX_CloudZen.CloudServices.EC2Connection.DTOs
{
    // ── File Listing ──────────────────────────────────────────────────────────

    public class FileListResponse
    {
        public string CurrentPath { get; set; } = string.Empty;
        public string ParentPath { get; set; } = string.Empty;
        public List<FileEntryInfo> Entries { get; set; } = new();
    }

    public class FileEntryInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public bool IsSymlink { get; set; }
        public long Size { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string ModifiedAt { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string LinkTarget { get; set; } = string.Empty;
    }

    // ── File Read / Write ─────────────────────────────────────────────────────

    public class FileReadResponse
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsBinary { get; set; }
        public long Size { get; set; }
        public string Encoding { get; set; } = "utf-8";
    }

    public record FileWriteRequest(
        string SessionId,
        string Path,
        string Content,
        bool Append = false
    );

    // ── File Operations ───────────────────────────────────────────────────────

    public record FileDeleteRequest(
        string SessionId,
        string Path,
        bool Recursive = false
    );

    public record FileMkdirRequest(
        string SessionId,
        string Path,
        bool CreateParents = true
    );

    public record FileRenameRequest(
        string SessionId,
        string OldPath,
        string NewPath
    );

    public record FileCopyRequest(
        string SessionId,
        string SourcePath,
        string DestinationPath
    );

    public class FileOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ── File Download ─────────────────────────────────────────────────────────

    public class FileDownloadResponse
    {
        public string Path { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentBase64 { get; set; } = string.Empty;
        public long Size { get; set; }
        public string MimeType { get; set; } = "application/octet-stream";
    }

    // ── File Search ───────────────────────────────────────────────────────────

    public record FileSearchRequest(
        string SessionId,
        string Directory,
        string Pattern,
        bool CaseSensitive = false,
        int MaxResults = 50
    );

    public class FileSearchResponse
    {
        public string Directory { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public List<FileEntryInfo> Results { get; set; } = new();
        public int TotalFound { get; set; }
    }

    // ── System Info ───────────────────────────────────────────────────────────

    public class SystemInfoResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string OsRelease { get; set; } = string.Empty;
        public string Kernel { get; set; } = string.Empty;
        public string Uptime { get; set; } = string.Empty;
        public string Cpu { get; set; } = string.Empty;
        public string Memory { get; set; } = string.Empty;
        public string DiskUsage { get; set; } = string.Empty;
        public string HomeDirectory { get; set; } = string.Empty;
        public string RemoteUser { get; set; } = string.Empty;
        public string Shell { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    }
}
