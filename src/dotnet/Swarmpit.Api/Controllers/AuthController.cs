using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Api.Auth;
using Swarmpit.Api.Data.CouchDb;
using Swarmpit.Api.Models;

namespace Swarmpit.Api.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserRepository _users;
    private readonly JwtService _jwt;

    public AuthController(UserRepository users, JwtService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    [AllowAnonymous]
    [HttpPost("/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var valid = await _users.VerifyPasswordAsync(request.Username, request.Password);
        if (!valid)
            return Unauthorized(new { error = "Invalid credentials" });

        var user = await _users.GetByUsernameAsync(request.Username);
        var token = await _jwt.GenerateTokenAsync(user!);
        return Ok(new LoginResponse { Token = token });
    }

    [AllowAnonymous]
    [HttpPost("/initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializeRequest request)
    {
        var existing = await _users.GetAllAsync();
        if (existing.Count > 0)
            return BadRequest(new { error = "Already initialized" });

        await _users.CreateAsync(request.Username, request.Password, "admin");
        return StatusCode(201);
    }

    [Authorize]
    [HttpGet("/api/me")]
    public async Task<IActionResult> Me()
    {
        var username = User.FindFirst("sub")?.Value;
        if (username == null) return Unauthorized();

        var user = await _users.GetByUsernameAsync(username);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Username,
            user.Role,
            user.Email
        });
    }
}
