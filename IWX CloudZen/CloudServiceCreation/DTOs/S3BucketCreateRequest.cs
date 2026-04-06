namespace IWX_CloudZen.CloudServiceCreation.DTOs
{
    public class S3BucketCreateRequest
    {
        public int CloudAccountId { get; set; }

        public string BucketName { get; set; } = null!;

        //public string Region { get; set; } = null!;
    }
}
