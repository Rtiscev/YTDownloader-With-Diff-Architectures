namespace YTDownloader.Backend.DTOs.Responses;

public class UserDto
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
