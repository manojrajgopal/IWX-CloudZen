using Amazon.ECR;
using IWX_CloudZen.CloudAccounts.DTOs;
using Amazon;
using Amazon.ECR.Model;

namespace IWX_CloudZen.CloudDeployments.Pipeline
{
    public class EcrPushService
    {
        public async Task<string> CreateRepo(CloudConnectionSecrets account, string repo)
        {
            var client = new AmazonECRClient(account.AccessKey, account.SecretKey, RegionEndpoint.GetBySystemName(account.Region));

            var repos = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest());

            if(repos.Repositories.Any(x => x.RepositoryName == repo))
                return repos.Repositories.First(x => x.RepositoryName == repo).RepositoryUri;

            var result = await client.CreateRepositoryAsync(
                new CreateRepositoryRequest
                {
                    RepositoryName = repo
                }
            );

            return result.Repository.RepositoryUri;
        }
    }
}
