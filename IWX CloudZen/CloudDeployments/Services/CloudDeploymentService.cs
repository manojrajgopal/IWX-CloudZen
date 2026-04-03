using IWX_CloudZen.Data;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudDeployments.Factory;
using IWX_CloudZen.CloudDeployments.Entities;

namespace IWX_CloudZen.CloudDeployments.Services
{
    public class CloudDeploymentService
    {
        private readonly AppDbContext _context;
        private readonly CloudAccountService _accounts;

        public CloudDeploymentService(AppDbContext context, CloudAccountService accounts)
        {
            _context = context;
            _accounts = accounts;
        }

        public async Task<CloudDeployment> Deploy(string user, string name, string type, int accountId, IFormFile package)
        {
            var account = await _accounts.ResolveCredentialsAsync(user, accountId) ?? throw new Exception("Cloud account not found.");

            var provider = DeploymentProviderFactory.Get(account.Provider);

            var result = await provider.Deploy(account, package, name, type);

            var entity = new CloudDeployment
            {
                Name = name,
                Provider = account.Provider,
                DeploymentType = type,
                CloudAccountId = accountId,
                Status = result.Status,
                ImageUrl = result.ImageUrl,
                ServiceName = result.ServiceName,
                ClusterName = result.ClusterName,
                HealthUrl = result.HealthUrl,
                LogsGroup = result.LogsGroup,
                PackagePath = package.FileName,
                UploadedBy = user,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            // entity.HealthUrl = "http://" + serviceUrl;

            _context.CloudDeployments.Add(entity);

            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task Stop(string user, int id)
        {
            var dep = _context.CloudDeployments.First(x => x.Id == id);

            var account = await _accounts.ResolveCredentialsAsync(user, dep.CloudAccountId) ?? throw new Exception("Cloud account not found."); ;

            var provider = DeploymentProviderFactory.Get(dep.Provider);

            await provider.Stop(account, dep.Name);

            dep.Status = "Stoped";
            dep.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task Restart(string user, int id)
        {
            var dep = _context.CloudDeployments.First(x => x.Id == id);
            var account = await _accounts.ResolveCredentialsAsync(user, dep.CloudAccountId)
                ?? throw new Exception("Cloud account not found.");

            var provider = DeploymentProviderFactory.Get(dep.Provider);
            await provider.Restart(account, dep.Name);

            dep.Status = "Running";
            dep.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public List<CloudDeployment> GetDeployments(string user)
        {
            return _context.CloudDeployments.Where(x => x.UploadedBy == user).OrderByDescending(x =>  x.CreatedAt).ToList();
        }
    }
}
