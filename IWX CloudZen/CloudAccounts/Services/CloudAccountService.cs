using Microsoft.EntityFrameworkCore;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Entities;
using IWX_CloudZen.CloudAccounts.Factory;
using IWX_CloudZen.CloudAccounts.Interfaces;
using IWX_CloudZen.Data;

namespace IWX_CloudZen.CloudAccounts.Services
{
    public class CloudAccountService
    {
        private readonly AppDbContext _context;
        private readonly ICloudSecretProtector _protector;

        public CloudAccountService(
            AppDbContext context,
            ICloudSecretProtector protector)
        {
            _context = context;
            _protector = protector;
        }

        public async Task<List<CloudProviderOption>> GetSupportedProvidersAsync()
        {
            return await Task.FromResult(CloudProviderFactory.GetSupportedProviders());
        }

        public async Task<CloudAccountResponse> ConnectAccountAsync(
            string userEmail,
            ConnectCloudRequest request)
        {
            var provider = CloudProviderFactory.GetProvider(request.Provider);

            var valid = await provider.ValidateConnectionAsync(request);
            if (!valid)
                throw new InvalidOperationException("Invalid cloud credentials.");

            var exists = await _context.CloudAccounts.AnyAsync(x =>
                x.UserEmail == userEmail &&
                x.Provider == request.Provider.Trim() &&
                x.AccountName == request.AccountName.Trim());

            if (exists)
                throw new InvalidOperationException("Account name already exists for this provider.");

            var hasAnyAccount = await _context.CloudAccounts.AnyAsync(x => x.UserEmail == userEmail);

            if (request.MakeDefault)
            {
                var currentDefaults = await _context.CloudAccounts
                    .Where(x => x.UserEmail == userEmail && x.IsDefault)
                    .ToListAsync();

                foreach (var item in currentDefaults)
                    item.IsDefault = false;
            }

            var account = new CloudAccount
            {
                UserEmail = userEmail,
                Provider = request.Provider.Trim().ToUpperInvariant(),
                AccountName = request.AccountName.Trim(),
                AccessKeyEncrypted = _protector.Protect(request.AccessKey ?? string.Empty),
                SecretKeyEncrypted = _protector.Protect(request.SecretKey ?? string.Empty),
                TenantIdEncrypted = _protector.Protect(request.TenantId ?? string.Empty),
                ClientIdEncrypted = _protector.Protect(request.ClientId ?? string.Empty),
                ClientSecretEncrypted = _protector.Protect(request.ClientSecret ?? string.Empty),
                Region = request.Region?.Trim(),
                IsDefault = request.MakeDefault || !hasAnyAccount,
                CreatedAt = DateTime.UtcNow,
                LastValidatedAt = DateTime.UtcNow
            };

            _context.CloudAccounts.Add(account);
            await _context.SaveChangesAsync();

            return Map(account);
        }

        public async Task<List<CloudAccountResponse>> GetUserAccountsAsync(string userEmail)
        {
            var accounts = await _context.CloudAccounts
                .Where(x => x.UserEmail == userEmail)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();

            return accounts.Select(Map).ToList();
        }

        public async Task<CloudAccountResponse?> GetDefaultAccountAsync(string userEmail)
        {
            var account = await _context.CloudAccounts
                .FirstOrDefaultAsync(x => x.UserEmail == userEmail && x.IsDefault);

            if (account == null)
            {
                account = await _context.CloudAccounts
                    .FirstOrDefaultAsync(x => x.UserEmail == userEmail);
            }

            return account == null ? null : Map(account);
        }

        public async Task<CloudAccountResponse?> SetDefaultAccountAsync(string userEmail, int accountId)
        {
            var accounts = await _context.CloudAccounts
                .Where(x => x.UserEmail == userEmail)
                .ToListAsync();

            var selected = accounts.FirstOrDefault(x => x.Id == accountId);
            if (selected == null)
                return null;

            foreach (var account in accounts)
                account.IsDefault = account.Id == accountId;

            await _context.SaveChangesAsync();

            return Map(selected);
        }

        public async Task<CloudConnectionSecrets?> ResolveCredentialsAsync(string userEmail, int? accountId = null)
        {
            CloudAccount? account;

            if (accountId.HasValue)
            {
                account = await _context.CloudAccounts.FirstOrDefaultAsync(x =>
                    x.UserEmail == userEmail && x.Id == accountId.Value);
            }
            else
            {
                account = await _context.CloudAccounts.FirstOrDefaultAsync(x =>
                    x.UserEmail == userEmail && x.IsDefault);

                account ??= await _context.CloudAccounts.FirstOrDefaultAsync(x =>
                    x.UserEmail == userEmail);
            }

            if (account == null)
                return null;

            return new CloudConnectionSecrets
            {
                Id = account.Id,
                UserEmail = account.UserEmail,
                Provider = account.Provider,
                AccountName = account.AccountName,
                AccessKey = _protector.Unprotect(account.AccessKeyEncrypted),
                SecretKey = _protector.Unprotect(account.SecretKeyEncrypted),
                TenantId = _protector.Unprotect(account.TenantIdEncrypted ?? string.Empty),
                ClientId = _protector.Unprotect(account.ClientIdEncrypted ?? string.Empty),
                ClientSecret = _protector.Unprotect(account.ClientSecretEncrypted ?? string.Empty),
                Region = account.Region,
                IsDefault = account.IsDefault
            };
        }

        private static CloudAccountResponse Map(CloudAccount account)
        {
            return new CloudAccountResponse
            {
                Id = account.Id,
                Provider = account.Provider,
                AccountName = account.AccountName,
                Region = account.Region,
                IsDefault = account.IsDefault,
                CreatedAt = account.CreatedAt,
                LastValidatedAt = account.LastValidatedAt
            };
        }
    }
}