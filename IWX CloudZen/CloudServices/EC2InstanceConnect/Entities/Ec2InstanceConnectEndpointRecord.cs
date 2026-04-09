using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Entities
{
    public class Ec2InstanceConnectEndpointRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string EndpointId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string SubnetId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string VpcId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [MaxLength(300)]
        public string DnsName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string NetworkInterfaceId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AvailabilityZone { get; set; } = string.Empty;

        [MaxLength(300)]
        public string FipsDnsName { get; set; } = string.Empty;

        public bool PreserveClientIp { get; set; }

        /// <summary>JSON array of security group IDs attached to this endpoint.</summary>
        public string? SecurityGroupIdsJson { get; set; }

        /// <summary>JSON object of tags attached to this endpoint.</summary>
        public string? TagsJson { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
