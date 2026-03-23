namespace IWX_CloudZen.CloudStorage.DTOs
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Folder { get; set; } = string.Empty;
        public int CloudAccountId { get; set; }
    }
}
