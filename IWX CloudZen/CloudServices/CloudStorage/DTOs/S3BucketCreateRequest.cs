namespace IWX_CloudZen.CloudServices.CloudStorage.DTOs
{
    public class S3BucketCreateRequest
    {
        public int CloudAccountId { get; set; }

        public string BucketName { get; set; } = null!;
    }
}
