namespace IWX_CloudZen.CloudStorage.Entities
{
    public class CloudFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string Provider { get; set; }
        public string Folder { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; }
        public int CloudAccountId { get; set; }
        public string UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
