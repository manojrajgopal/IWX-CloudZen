namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class SyncImagesResult
    {
        public string RepositoryName { get; set; } = string.Empty;
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<ImageResponse> Images { get; set; } = new();
    }
}
