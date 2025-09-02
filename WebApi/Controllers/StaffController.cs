using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Staff;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrBranchAdmin)]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class StaffController : ControllerBase
    {
        private readonly IStaffService _svc;
        public StaffController(IStaffService svc) => _svc = svc;

        [HttpGet("branches/{branchId:guid}/staff")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> List(Guid branchId, CancellationToken ct)
        {
            var items = await _svc.ListAsync(branchId, ct);
            return Ok(new { items });
        }

        [HttpPost("branches/{branchId:guid}/staff")]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Create(Guid branchId, [FromBody] StaffCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        [HttpPut("staff/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StaffUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpDelete("staff/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id });
        }

        [HttpPut("staff/{id:guid}/counters")]
        public async Task<IActionResult> UpdateCounters(Guid id, [FromBody] StaffCountersUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateCountersAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpPut("staff/{id:guid}/schedule")]
        public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] IEnumerable<StaffScheduleUpdateRequest> req, CancellationToken ct)
        {
            await _svc.UpdateScheduleAsync(id, req, ct);
            return Ok(new { id, status = "approved" });
        }
    }
}
