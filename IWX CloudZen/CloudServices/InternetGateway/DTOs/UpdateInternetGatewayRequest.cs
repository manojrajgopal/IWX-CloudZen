using System.ComponentModel.DataAnnotations;

namespace IWX_CloudZen.CloudServices.InternetGateway.DTOs
{
    public class UpdateInternetGatewayRequest
    {
        [MaxLength(256)]
        public string? Name { get; set; }
    }
}
