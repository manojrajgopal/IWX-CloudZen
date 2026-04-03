using System.Security.Claims;
using IWX_CloudZen.Authentication.DTOs.Auth;

namespace IWX_CloudZen.Authentication.Interfaces
{
    public interface IAuthService
    {
        Task<string> Register(RegisterRequest req);
        Task<object> Login(LoginRequest req);
        Object Me(ClaimsPrincipal user);
    }
}
