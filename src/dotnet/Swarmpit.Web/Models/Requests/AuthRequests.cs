namespace Swarmpit.Web.Models.Requests;

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

public class CreateUserRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Role { get; set; }
    public string? Email { get; set; }
}

public class UpdateUserRequest
{
    public string? Password { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
}
