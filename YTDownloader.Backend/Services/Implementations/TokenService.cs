using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using YTDownloader.Backend.Models;
using YTDownloader.Backend.Services.Interfaces;

namespace YTDownloader.Backend.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        try
        {
            var secret = _configuration["Jwt_Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("JWT Secret not configured");
                throw new InvalidOperationException("JWT Secret not configured");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.GivenName, user.FirstName ?? ""),
                new(ClaimTypes.Surname, user.LastName ?? ""),
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt_Issuer"],
                audience: _configuration["Jwt_Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("✓ Token generated for user {UserId}", user.Id);

            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate token for user {UserId}", user.Id);
            throw;
        }
    }

    public RefreshToken GenerateRefreshToken()
    {
        try
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(randomNumber);

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            _logger.LogInformation("✓ Refresh token generated");
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate refresh token");
            throw;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var secret = _configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(secret))
                return null;

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
                return null;

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate expired token");
            return null;
        }
    }
}
