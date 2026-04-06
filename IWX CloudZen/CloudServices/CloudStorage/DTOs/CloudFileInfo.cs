namespace IWX_CloudZen.CloudServices.CloudStorage.DTOs
{
    public class CloudFileInfo
    {
        public string Key { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ETag { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
