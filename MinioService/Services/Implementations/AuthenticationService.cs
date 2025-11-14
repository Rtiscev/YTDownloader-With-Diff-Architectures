using System.Text.Json;
using MinioService.Services.Interfaces;

namespace MinioService.Services.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool valid, string? error, bool isAdmin)> VerifyAuthAsync(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authHeader))
            return (false, "Unauthorized", false);

        if (!authHeader.StartsWith("Bearer "))
            return (false, "Invalid authorization format", false);

        var token = authHeader["Bearer ".Length..];
        if (string.IsNullOrWhiteSpace(token))
            return (false, "No token provided", false);

        try
        {
            string AuthenticationServiceUrl = _configuration["AUTH_SERVICE_URL"]
                ?? "http://apigateway:5000/api/auth/me";

            var httpClient = _httpClientFactory.CreateClient();
            var authRequest = new HttpRequestMessage(HttpMethod.Get, AuthenticationServiceUrl)
            {
                Headers = { { "Authorization", authHeader } }
            };

            var authResponse = await httpClient.SendAsync(authRequest);

            if (!authResponse.IsSuccessStatusCode)
            {
                return authResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? (false, "Unauthorized - Invalid token", false)
                    : (false, "Auth service error", false);
            }

            var json = await authResponse.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<JsonElement>(json);

            if (userData.ValueKind == JsonValueKind.Object)
            {
                if (userData.TryGetProperty("role", out var roleProp))
                    return (true, null, roleProp.GetString() == "Admin");

                if (userData.TryGetProperty("roles", out var rolesProp)
                    && rolesProp.ValueKind == JsonValueKind.Array)
                {
                    var isAdmin = rolesProp.EnumerateArray()
                        .Any(r => r.GetString() == "Admin");
                    return (true, null, isAdmin);
                }
            }

            return (true, null, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auth] Error verifying token");
            return (false, "Auth verification failed", false);
        }
    }
}
