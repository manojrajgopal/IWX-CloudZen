namespace IWX_CloudZen.CloudServiceCreation.DTOs
{
    public class AwsInfrastructureResult
    {
        public string Region { get; set; } = string.Empty;
        public string VpcId { get; set; } = string.Empty;
        public List<string> PublicSubnetIds { get; set; } = new();
        public string SecurityGroupId {  get; set; } = string.Empty;
        public string ClusterName {  get; set; } = string.Empty;
        public string LoadBalancerArn {  get; set; } = string.Empty;
        public string LoadBalancerDnsName {  get; set; } = string.Empty;
        public string TargetGroupArn {  get; set; } = string.Empty;
        public string ExecutionRoleArn {  get; set; } = string.Empty;
        public string LogGroupName {  get; set; } = string.Empty;
    }
}
