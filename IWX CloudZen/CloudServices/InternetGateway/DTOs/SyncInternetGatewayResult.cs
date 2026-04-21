namespace IWX_CloudZen.CloudServices.InternetGateway.DTOs
{
    public class SyncInternetGatewayResult
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public List<InternetGatewayResponse> InternetGateways { get; set; } = new();
    }
}
