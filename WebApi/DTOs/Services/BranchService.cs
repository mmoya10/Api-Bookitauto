using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Branches;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IBranchService
    {
        Task UpdateAsync(Guid branchId, BranchUpdateRequest req, CancellationToken ct);
        Task<IReadOnlyList<BranchScheduleDto>> GetSchedulesAsync(Guid branchId, CancellationToken ct);
        Task UpdateSchedulesAsync(Guid branchId, BranchScheduleUpdateRequest req, CancellationToken ct);
    }

    public sealed class BranchService : IBranchService
    {
        private readonly AppDbContext _db;
        public BranchService(AppDbContext db) => _db = db;

        // ===== Branch =====
        public async Task UpdateAsync(Guid branchId, BranchUpdateRequest req, CancellationToken ct)
        {
            var b = await _db.Branches.FirstOrDefaultAsync(x => x.Id == branchId, ct)
                ?? throw new KeyNotFoundException("Sucursal no encontrada.");

            if (req.Name is not null)       b.Name       = req.Name.Trim();
            if (req.Email is not null)      b.Email      = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            if (req.Phone is not null)      b.Phone      = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            if (req.Address is not null)    b.Address    = req.Address;
            if (req.City is not null)       b.City       = req.City;
            if (req.Province is not null)   b.Province   = req.Province;
            if (req.PostalCode is not null) b.PostalCode = req.PostalCode;

            if (req.Timezone is not null)
            {
                // Validación ligera (no lista blanca): solo que no sea vacío
                if (string.IsNullOrWhiteSpace(req.Timezone)) throw new InvalidOperationException("Timezone inválido.");
                b.Timezone = req.Timezone;
            }

            if (req.StartDate.HasValue) b.StartDate = req.StartDate.Value;
            if (req.EndDate.HasValue)   b.EndDate   = req.EndDate.Value;
            if (b.EndDate is not null && b.StartDate is not null && b.EndDate < b.StartDate)
                throw new InvalidOperationException("EndDate no puede ser anterior a StartDate.");

            if (req.Status is not null)
            {
                if (req.Status is not ("active" or "inactive"))
                    throw new InvalidOperationException("Status inválido (active|inactive).");
                b.Status = req.Status;
            }

            if (req.Slug is not null)
            {
                // Nota: ya tienes UNIQUE(businessId, slug). Aquí no cambiamos businessId.
                var dup = await _db.Branches.AnyAsync(x => x.BusinessId == b.BusinessId && x.Slug == req.Slug && x.Id != branchId, ct);
                if (dup) throw new InvalidOperationException("Slug ya existe para este negocio.");
                b.Slug = string.IsNullOrWhiteSpace(req.Slug) ? null : req.Slug.Trim();
            }

            b.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // ===== Schedules =====
        public async Task<IReadOnlyList<BranchScheduleDto>> GetSchedulesAsync(Guid branchId, CancellationToken ct)
        {
            return await _db.BranchSchedules
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .OrderBy(x => x.Weekday).ThenBy(x => x.StartTime)
                .Select(x => new BranchScheduleDto
                {
                    Id = x.Id,
                    Weekday = x.Weekday,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                })
                .ToListAsync(ct);
        }

        public async Task UpdateSchedulesAsync(Guid branchId, BranchScheduleUpdateRequest req, CancellationToken ct)
        {
            // Validaciones: weekday [0..6], fin>inicio, sin solapes por día
            foreach (var it in req.Items)
            {
                if (it.Weekday < 0 || it.Weekday > 6)
                    throw new InvalidOperationException("Weekday debe estar entre 0 y 6.");
                if (it.EndTime <= it.StartTime)
                    throw new InvalidOperationException("EndTime debe ser mayor que StartTime.");
            }

            var byDay = req.Items
                .GroupBy(x => x.Weekday)
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.EndTime)
                    .ToList());

            foreach (var (_, list) in byDay)
            {
                for (int i = 1; i < list.Count; i++)
                {
                    var prev = list[i - 1];
                    var curr = list[i];
                    // solape si el inicio actual es menor que el fin anterior
                    if (curr.StartTime < prev.EndTime)
                        throw new InvalidOperationException("Rangos horarios solapados en el mismo día.");
                }
            }

            // Reemplazo atómico de horarios de la sucursal
            var current = await _db.BranchSchedules.Where(x => x.BranchId == branchId).ToListAsync(ct);
            _db.BranchSchedules.RemoveRange(current);

            var toInsert = req.Items.Select(it => new Domain.Entities.BranchSchedule
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Weekday = it.Weekday,
                StartTime = it.StartTime,
                EndTime = it.EndTime
            });

            await _db.BranchSchedules.AddRangeAsync(toInsert, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
