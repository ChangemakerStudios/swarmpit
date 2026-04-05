using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Swarmpit.Web.Controllers;

[ApiController]
public class VersionController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/version")]
    public IActionResult Get()
    {
        return Ok(new
        {
            version = "2.1.0",
            revision = "dotnet"
        });
    }
}
