namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class SyncRepositoriesResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<RepositoryResponse> Repositories { get; set; } = new();
    }
}
