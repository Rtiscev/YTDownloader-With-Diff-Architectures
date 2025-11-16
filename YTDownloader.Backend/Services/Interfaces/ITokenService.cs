using System.Security.Claims;
using YTDownloader.Backend.Models;

namespace YTDownloader.Backend.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    RefreshToken GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
