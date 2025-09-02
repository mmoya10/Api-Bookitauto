using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.BookingSites;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Policies;
using WebApi.Infrastructure.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/branches/{branchId:guid}/booking-sites")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrBranchAdmin)]
    [ServiceFilter(typeof(RequireBranchHeaderFilter))]
    public sealed class BookingSitesController : ControllerBase
    {
        private readonly IBookingSiteService _svc;
        public BookingSitesController(IBookingSiteService svc) => _svc = svc;

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> List(Guid branchId, CancellationToken ct = default)
            => Ok(new { items = await _svc.ListAsync(branchId, ct) });

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.RequireBranchScope)]
        public async Task<IActionResult> Create(Guid branchId, [FromBody] BookingSiteCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(branchId, req, ct);
            return Ok(new { id });
        }

        [HttpPut("/api/booking-sites/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BookingSiteUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpDelete("/api/booking-sites/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { id, status = "archived" });
        }

        // Steps
        [HttpGet("/api/booking-sites/{siteId:guid}/steps")]
        public async Task<IActionResult> Steps(Guid siteId, CancellationToken ct)
            => Ok(new { items = await _svc.GetStepsAsync(siteId, ct) });

        public sealed class StepCreateRequest { public string Step { get; init; } = "service"; public int Position { get; init; } }
        [HttpPost("/api/booking-sites/{siteId:guid}/steps")]
        public async Task<IActionResult> AddStep(Guid siteId, [FromBody] StepCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.AddStepAsync(siteId, req.Step, req.Position, ct);
            return Ok(new { id });
        }

        public sealed class StepUpdateRequest { public string? Step { get; init; } public int? Position { get; init; } }
        [HttpPut("/api/booking-site-steps/{id:guid}")]
        public async Task<IActionResult> UpdateStep(Guid id, [FromBody] StepUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateStepAsync(id, req.Step, req.Position, ct);
            return Ok(new { id });
        }

        [HttpDelete("/api/booking-site-steps/{id:guid}")]
        public async Task<IActionResult> DeleteStep(Guid id, CancellationToken ct)
        {
            await _svc.DeleteStepAsync(id, ct);
            return Ok(new { id });
        }

        // Services in site
        [HttpGet("/api/booking-sites/{siteId:guid}/services")]
        public async Task<IActionResult> SiteServices(Guid siteId, CancellationToken ct)
            => Ok(new { items = await _svc.GetSiteServicesAsync(siteId, ct) });

        public sealed class AddSiteServiceRequest { public Guid ServiceId { get; init; } public bool Active { get; init; } = true; public int Position { get; init; } }
        [HttpPost("/api/booking-sites/{siteId:guid}/services")]
        public async Task<IActionResult> AddSiteService(Guid siteId, [FromBody] AddSiteServiceRequest req, CancellationToken ct)
        {
            await _svc.AddSiteServiceAsync(siteId, req.ServiceId, req.Active, req.Position, ct);
            return Ok(new { siteId });
        }

        public sealed class UpdateSiteServiceRequest { public bool? Active { get; init; } public int? Position { get; init; } }
        [HttpPut("/api/booking-site-services/{siteId:guid}/{serviceId:guid}")]
        public async Task<IActionResult> UpdateSiteService(Guid siteId, Guid serviceId, [FromBody] UpdateSiteServiceRequest req, CancellationToken ct)
        {
            await _svc.UpdateSiteServiceAsync(siteId, serviceId, req.Active, req.Position, ct);
            return Ok(new { siteId, serviceId });
        }

        [HttpDelete("/api/booking-site-services/{siteId:guid}/{serviceId:guid}")]
        public async Task<IActionResult> DeleteSiteService(Guid siteId, Guid serviceId, CancellationToken ct)
        {
            await _svc.DeleteSiteServiceAsync(siteId, serviceId, ct);
            return Ok(new { siteId, serviceId });
        }

        // Forms
        [HttpGet("/api/booking-sites/{siteId:guid}/forms")]
        public async Task<IActionResult> Forms(Guid siteId, CancellationToken ct)
            => Ok(new { items = await _svc.GetFormsAsync(siteId, ct) });

        [HttpPost("/api/booking-sites/{siteId:guid}/forms")]
        public async Task<IActionResult> CreateForm(Guid siteId, [FromBody] BookingFormCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateFormAsync(siteId, req, ct);
            return Ok(new { id });
        }

        [HttpPost("/api/booking-forms/{formId:guid}/fields")]
        public async Task<IActionResult> AddField(Guid formId, [FromBody] BookingFormFieldCreateRequest req, CancellationToken ct)
        {
            var id = await _svc.AddFieldAsync(formId, req, ct);
            return Ok(new { id });
        }

        [HttpPut("/api/booking-form-fields/{id:guid}")]
        public async Task<IActionResult> UpdateField(Guid id, [FromBody] BookingFormFieldUpdateRequest req, CancellationToken ct)
        {
            await _svc.UpdateFieldAsync(id, req, ct);
            return Ok(new { id });
        }

        [HttpDelete("/api/booking-form-fields/{id:guid}")]
        public async Task<IActionResult> DeleteField(Guid id, CancellationToken ct)
        {
            await _svc.DeleteFieldAsync(id, ct);
            return Ok(new { id });
        }
    }
}
