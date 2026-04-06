namespace IWX_CloudZen.CloudServices.VPC.DTOs
{
    public class SyncVpcResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<VpcResponse> Vpcs { get; set; } = new();
    }
}
