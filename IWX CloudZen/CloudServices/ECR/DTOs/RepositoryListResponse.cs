namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public class RepositoryListResponse
    {
        public int TotalRepositories { get; set; }
        public List<RepositoryResponse> Repositories { get; set; } = new();
    }
}
