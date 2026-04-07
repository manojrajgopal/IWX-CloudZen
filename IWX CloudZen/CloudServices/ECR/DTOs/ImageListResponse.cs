namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class ImageListResponse
    {
        public string RepositoryName { get; set; } = string.Empty;
        public int TotalImages { get; set; }
        public List<ImageResponse> Images { get; set; } = new();
    }
}
