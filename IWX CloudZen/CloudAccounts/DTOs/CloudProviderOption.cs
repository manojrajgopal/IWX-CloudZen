namespace IWX_CloudZen.CloudAccounts.DTOs
{
    public class CloudProviderOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string[] RequiredFields { get; set; } = [];
    }
}