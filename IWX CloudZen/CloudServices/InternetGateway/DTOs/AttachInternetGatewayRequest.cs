using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.InternetGateway.DTOs
{
    public class AttachInternetGatewayRequest
    {
        [Required, MaxLength(100)]
        public string VpcId { get; set; } = string.Empty;
    }
}
