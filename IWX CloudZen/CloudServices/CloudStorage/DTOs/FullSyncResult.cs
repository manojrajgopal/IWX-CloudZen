namespace IWX_CloudZen.CloudServices.CloudStorage.DTOs
{
    public class BucketFileSyncResult
    {
        public BucketResponse Bucket { get; set; } = new();
        public int FilesAdded { get; set; }
        public int FilesUpdated { get; set; }
        public int FilesRemoved { get; set; }
        public List<FileResponse> Files { get; set; } = new();
    }

    public class FullSyncResult
    {
        public int BucketsAdded { get; set; }
        public int BucketsUpdated { get; set; }
        public int BucketsRemoved { get; set; }
        public List<BucketFileSyncResult> Buckets { get; set; } = new();
    }
}
