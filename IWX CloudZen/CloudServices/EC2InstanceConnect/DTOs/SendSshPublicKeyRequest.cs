namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public record SendSshPublicKeyRequest(
        string InstanceId,
        string InstanceOsUser,
        int KeyPairDbId,
        string? AvailabilityZone = null
    );
}
