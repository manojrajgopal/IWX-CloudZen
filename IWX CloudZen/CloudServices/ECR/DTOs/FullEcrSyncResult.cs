namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class RepositoryImageSyncResult
    {
        public RepositoryResponse Repository { get; set; } = new();
        public int ImagesAdded { get; set; }
        public int ImagesUpdated { get; set; }
        public int ImagesRemoved { get; set; }
        public List<ImageResponse> Images { get; set; } = new();
    }

    public class FullEcrSyncResult
    {
        public int RepositoriesAdded { get; set; }
        public int RepositoriesUpdated { get; set; }
        public int RepositoriesRemoved { get; set; }
        public List<RepositoryImageSyncResult> Repositories { get; set; } = new();
    }
}
