using IWX_CloudZen.CloudAccounts.Entities;
using IWX_CloudZen.CloudAccounts.Factory;
using IWX_CloudZen.Data;

namespace IWX_CloudZen.CloudAccounts.Services
{
    public class CloudAccountService
    {
        private readonly AppDbContext _context;

        public CloudAccountService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<string>
        ConnectAccount(
            CloudAccount account)
        {

            var provider =
            CloudProviderFactory
            .GetProvider(account.Provider);

            var valid =
            await provider
            .ValidateConnection(account);

            if (!valid)
                return "Invalid credentials";

            _context.CloudAccounts
            .Add(account);

            await _context
            .SaveChangesAsync();

            return "Connected";

        }

        public List<CloudAccount>
        GetUserAccounts(string user)
        {

            return _context
            .CloudAccounts
            .Where(x => x.UserEmail == user)
            .ToList();

        }
    }
}
