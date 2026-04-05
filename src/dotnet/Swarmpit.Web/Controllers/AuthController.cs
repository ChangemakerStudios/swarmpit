using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Users;
using Swarmpit.Core.Infrastructure.Auth;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
public class AuthController(IUserRepository users, JwtService jwt) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var valid = await users.VerifyPasswordAsync(request.Username, request.Password);
        if (!valid)
            return Unauthorized(new { error = "Invalid credentials" });

        var user = await users.GetByUsernameAsync(request.Username);
        var token = await jwt.GenerateTokenAsync(user!);
        return Ok(new LoginResponse { Token = token });
    }

    [AllowAnonymous]
    [HttpPost("/initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializeRequest request)
    {
        var existing = await users.GetAllAsync();
        if (existing.Count > 0)
            return BadRequest(new { error = "Already initialized" });

        await users.CreateAsync(request.Username, request.Password, "admin");
        return StatusCode(201);
    }

    [Authorize]
    [HttpGet("/api/me")]
    public async Task<IActionResult> Me()
    {
        var username = User.FindFirst("sub")?.Value;
        if (username == null) return Unauthorized();

        var user = await users.GetByUsernameAsync(username);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Username,
            user.Role,
            user.Email
        });
    }
}
