namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public record CreateRepositoryRequest(
        string RepositoryName,
        string ImageTagMutability = "MUTABLE",
        bool ScanOnPush = false,
        string EncryptionType = "AES256"
    );
}
