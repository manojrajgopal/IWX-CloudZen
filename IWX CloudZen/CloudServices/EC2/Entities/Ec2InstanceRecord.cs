using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.EC2.Entities
{
    public class Ec2InstanceRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string InstanceId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string InstanceType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PublicIpAddress { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PrivateIpAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string VpcId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SubnetId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ImageId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string KeyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Architecture { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Platform { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Monitoring { get; set; } = string.Empty;

        public bool EbsOptimized { get; set; }

        /// <summary>JSON array of security group IDs attached to this instance.</summary>
        public string? SecurityGroupsJson { get; set; }

        /// <summary>JSON object of tags attached to this instance.</summary>
        public string? TagsJson { get; set; }

        public DateTime? LaunchTime { get; set; }

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
