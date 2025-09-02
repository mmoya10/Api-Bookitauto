// Infrastructure/Services/Platform/PlatformTicketService.cs
using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Platform.Tickets;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services.Platform
{
    public interface IPlatformTicketService
    {
        Task<IReadOnlyList<TicketDto>> ListAsync(string? status, string? severity, CancellationToken ct);
        Task<Guid> CreateAsync(TicketCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid id, TicketUpdateRequest req, CancellationToken ct);
        Task CloseAsync(Guid id, CancellationToken ct);
    }

    public sealed class PlatformTicketService : IPlatformTicketService
    {
        private readonly AppDbContext _db;
        public PlatformTicketService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<TicketDto>> ListAsync(string? status, string? severity, CancellationToken ct)
        {
            var q = _db.SupportTickets.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(t => t.Status == status);

            if (!string.IsNullOrWhiteSpace(severity))
                q = q.Where(t => t.Severity == severity);

            return await q
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    BusinessId = t.BusinessId,
                    BranchId = t.BranchId,
                    Subject = t.Subject,
                    Message = t.Message,
                    Severity = t.Severity,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> CreateAsync(TicketCreateRequest req, CancellationToken ct)
        {
            var t = new WebApi.Domain.Entities.SupportTicket
            {
                Id = Guid.NewGuid(),
                BusinessId = req.BusinessId,
                BranchId = req.BranchId,
                Subject = req.Subject,
                Message = req.Message,
                Severity = req.Severity,
                Status = "open",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.SupportTickets.Add(t);
            await _db.SaveChangesAsync(ct);
            return t.Id;
        }

        public async Task UpdateAsync(Guid id, TicketUpdateRequest req, CancellationToken ct)
        {
            var t = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Ticket no encontrado.");
            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                t.Status = req.Status!;
                if (req.Status == "resolved") t.ResolvedAt = DateTimeOffset.UtcNow;
            }
            if (!string.IsNullOrWhiteSpace(req.ResolutionNote))
                t.ResolutionNote = req.ResolutionNote;
            t.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task CloseAsync(Guid id, CancellationToken ct)
        {
            var t = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Ticket no encontrado.");
            t.Status = "closed";
            t.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}
