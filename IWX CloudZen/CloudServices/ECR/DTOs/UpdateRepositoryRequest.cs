namespace IWX_CloudZen.CloudServices.ECR.DTOs
{
    public record UpdateRepositoryRequest(
        string? ImageTagMutability,
        bool? ScanOnPush
    );
}
