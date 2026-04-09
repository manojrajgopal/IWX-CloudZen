using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Entities
{
    public class Ec2InstanceConnectSessionRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string InstanceId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string InstanceOsUser { get; set; } = string.Empty;

        [MaxLength(50)]
        public string AvailabilityZone { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SessionType { get; set; } = string.Empty;  // SSH or SerialConsole

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;  // Success, Failed

        [MaxLength(200)]
        public string RequestId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Provider { get; set; } = string.Empty;

        public int CloudAccountId { get; set; }

        [Required, MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
