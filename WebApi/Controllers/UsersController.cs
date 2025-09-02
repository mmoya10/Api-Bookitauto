using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class UsersController : ControllerBase
    {
        [HttpGet("branches/{branchId:guid}/users")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        [Authorize(Policy = "Perm.Users.View")]
        public IActionResult Search(Guid branchId, [FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int size = 20)
            => Ok(new { items = Array.Empty<object>(), page, size });

        [HttpGet("users/{id:guid}")]
        [Authorize(Policy = "Perm.Users.View")]
        public IActionResult Detail(Guid id) => Ok(new { id });

        [HttpPut("users/{id:guid}")]
        [Authorize(Policy = "Perm.Users.Manage")]
        public IActionResult Update(Guid id) => Ok(new { id });
    }
}
