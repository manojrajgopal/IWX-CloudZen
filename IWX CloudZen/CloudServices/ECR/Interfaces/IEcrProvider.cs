using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudServices.ECR.DTOs;

namespace IWX_CloudZen.CloudServices.ECR.Interfaces
{
    public interface IEcrProvider
    {
        // ---- Repositories ----
        Task<List<CloudRepositoryInfo>> FetchAllRepositories(CloudConnectionSecrets account);

        Task<CloudRepositoryInfo> CreateRepository(
            CloudConnectionSecrets account,
            string repositoryName,
            string imageTagMutability,
            bool scanOnPush,
            string encryptionType);

        Task<CloudRepositoryInfo> UpdateRepository(
            CloudConnectionSecrets account,
            string repositoryName,
            string? imageTagMutability,
            bool? scanOnPush);

        Task DeleteRepository(CloudConnectionSecrets account, string repositoryName, bool force);

        // ---- Images ----
        Task<List<CloudImageInfo>> FetchAllImages(CloudConnectionSecrets account, string repositoryName);

        Task DeleteImage(CloudConnectionSecrets account, string repositoryName, string imageDigest);
    }
}
