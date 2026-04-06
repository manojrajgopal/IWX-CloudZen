namespace IWX_CloudZen.CloudServices.CloudStorage.DTOs
{
    public class FileSyncResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<FileResponse> Files { get; set; } = new();
    }
}
