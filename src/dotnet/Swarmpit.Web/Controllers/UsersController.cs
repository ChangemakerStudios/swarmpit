using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swarmpit.Core.Application.Users;
using Swarmpit.Web.Models.Requests;

namespace Swarmpit.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(IUserRepository users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var allUsers = await users.GetAllAsync();
        return Ok(allUsers.Select(u => new { u.Username, u.Role, u.Email, HasApiToken = u.ApiToken != null }));
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> Get(string username)
    {
        var user = await users.GetByUsernameAsync(username);
        if (user == null) return NotFound();
        return Ok(new { user.Username, user.Role, user.Email, HasApiToken = user.ApiToken != null });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var existing = await users.GetByUsernameAsync(request.Username);
        if (existing != null) return Conflict(new { error = "User already exists" });

        var id = await users.CreateAsync(request.Username, request.Password, request.Role ?? "user", request.Email);
        return Ok(new { id, username = request.Username });
    }

    [HttpPut("{username}")]
    public async Task<IActionResult> Update(string username, [FromBody] UpdateUserRequest request)
    {
        var existing = await users.GetByUsernameAsync(username);
        if (existing == null) return NotFound();

        await users.UpdateAsync(username, request.Password, request.Role, request.Email);
        return Ok(new { username });
    }

    [HttpDelete("{username}")]
    public async Task<IActionResult> Delete(string username)
    {
        var existing = await users.GetByUsernameAsync(username);
        if (existing == null) return NotFound();

        if (User.Identity?.Name == username)
            return BadRequest(new { error = "Cannot delete your own account" });

        await users.DeleteAsync(username);
        return NoContent();
    }

    [HttpPost("{username}/token")]
    public async Task<IActionResult> GenerateToken(string username)
    {
        var existing = await users.GetByUsernameAsync(username);
        if (existing == null) return NotFound();

        var token = await users.GenerateApiTokenAsync(username);
        return Ok(new { token });
    }

    [HttpDelete("{username}/token")]
    public async Task<IActionResult> RevokeToken(string username)
    {
        var existing = await users.GetByUsernameAsync(username);
        if (existing == null) return NotFound();

        await users.RevokeApiTokenAsync(username);
        return NoContent();
    }
}
