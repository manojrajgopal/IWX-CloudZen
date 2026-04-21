using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.InternetGateway.DTOs
{
    public class CreateInternetGatewayRequest
    {
        [Required, MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional VPC ID to attach the internet gateway to immediately after creation.
        /// </summary>
        [MaxLength(100)]
        public string? VpcId { get; set; }
    }
}
