namespace IWX_CloudZen.CloudAccounts.Interfaces
{
    public interface ICloudSecretProtector
    {
        string Protect(string plainText);
        string Unprotect(string protectedText);
    }
}