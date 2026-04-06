namespace IWX_CloudZen.CloudServices.CloudStorage.DTOs
{
    public class BucketSyncResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<BucketResponse> Buckets { get; set; } = new();
    }
}
