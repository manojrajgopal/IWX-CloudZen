using Microsoft.AspNetCore.DataProtection;
using IWX_CloudZen.CloudAccounts.Interfaces;

namespace IWX_CloudZen.CloudAccounts.Services
{
    public class CloudSecretProtector : ICloudSecretProtector
    {
        private readonly IDataProtector _protector;

        public CloudSecretProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("IWXCloudZen.CloudAccounts.Credentials.v1");
        }

        public string Protect(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            return _protector.Protect(plainText);
        }

        public string Unprotect(string protectedText)
        {
            if (string.IsNullOrWhiteSpace(protectedText))
                return string.Empty;

            return _protector.Unprotect(protectedText);
        }
    }
}