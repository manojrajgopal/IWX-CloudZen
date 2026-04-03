namespace IWX_CloudZen.CloudDeployments.DTOs
{
    public class DeploymentRequest
    {
        public string Name { get; set; }
        public string DeploymentType { get; set; } // Container | Python | API | WebApp
        public int CloudAccountId { get; set; }
        public string? GitRepository { get; set; }
        public IFormFile Package { get; set; }
        public string? Branch { get; set; }
        public bool AutoDeploy { get; set;  }
    }
}
