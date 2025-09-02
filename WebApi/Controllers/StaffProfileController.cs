using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Staff;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/staff/me")]
    [Authorize] // cualquier staff autenticado
    public sealed class StaffProfileController : ControllerBase
    {
        private readonly IStaffProfileService _svc;
        public StaffProfileController(IStaffProfileService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Me(CancellationToken ct) 
            => Ok(await _svc.GetMeAsync(ct));

        [HttpPut]
        public async Task<IActionResult> UpdateMe([FromBody] StaffMeUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateMeAsync(req, ct);
            return Ok(new { ok = true });
        }

        [HttpGet("schedule")]
        public async Task<IActionResult> MySchedule(CancellationToken ct)
            => Ok(await _svc.GetMyScheduleAsync(ct));

        [HttpPut("schedule")]
        public async Task<IActionResult> RequestScheduleChange([FromBody] StaffScheduleChangeRequest req, CancellationToken ct)
        {
            var requestId = await _svc.RequestScheduleChangeAsync(req, ct);
            return Ok(new { requestId, status = "pending" });
        }

        [HttpGet("vacations")]
        public async Task<IActionResult> Counters(CancellationToken ct)
            => Ok(await _svc.GetCountersAsync(ct));

        [HttpGet("absences")]
        public async Task<IActionResult> MyAbsences(CancellationToken ct)
            => Ok(new { items = await _svc.MyAbsencesAsync(ct) });

        [HttpPost("absences")]
        public async Task<IActionResult> CreateAbsence([FromBody] AbsenceCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAbsenceAsync(req, ct);
            return Ok(new { id, status = "pending" });
        }
    }
}
