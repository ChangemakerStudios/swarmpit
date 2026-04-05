namespace Swarmpit.Core.Application.Users;

public interface IUserRepository
{
    Task<UserDoc?> GetByUsernameAsync(string username);
    Task<List<UserDoc>> GetAllAsync();
    Task<string> CreateAsync(string username, string password, string role, string? email = null);
    Task<bool> VerifyPasswordAsync(string username, string password);
}

public class UserDoc
{
    public string Id { get; set; } = "";
    public string Rev { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "user";
    public string? Email { get; set; }
    public ApiTokenDoc? ApiToken { get; set; }

    public bool IsAdmin => Role == "admin";
}

public class ApiTokenDoc
{
    public string? Jti { get; set; }
}
