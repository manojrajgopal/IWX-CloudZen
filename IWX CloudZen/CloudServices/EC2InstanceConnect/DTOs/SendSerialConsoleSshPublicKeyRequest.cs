namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs
{
    public record SendSerialConsoleSshPublicKeyRequest(
        string InstanceId,
        int KeyPairDbId,
        string? SerialPort = null
    );
}
