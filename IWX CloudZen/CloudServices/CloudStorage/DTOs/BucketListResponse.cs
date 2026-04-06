namespace IWX_CloudZen.CloudServices.CloudStorage.DTOs
{
    public class BucketListResponse
    {
        public List<BucketResponse> Buckets { get; set; } = new();
    }
}
