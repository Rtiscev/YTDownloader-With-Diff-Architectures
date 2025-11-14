namespace MinioService.Services.Interfaces;

public interface IAuthenticationService
{
    Task<(bool valid, string? error, bool isAdmin)> VerifyAuthAsync(HttpRequest request);
}
