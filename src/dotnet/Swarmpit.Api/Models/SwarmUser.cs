namespace Swarmpit.Api.Models;

public class SwarmUser
{
    public string? Id { get; set; }
    public string? Rev { get; set; }
    public string Type { get; set; } = "user";
    public string Username { get; set; } = "";
    public string? Password { get; set; }
    public string Role { get; set; } = "admin";
    public string? Email { get; set; }
    public ApiToken? ApiToken { get; set; }
}

public class ApiToken
{
    public string? Jti { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
}

public class InitializeRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
