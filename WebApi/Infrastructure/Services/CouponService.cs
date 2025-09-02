using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Coupons;
using WebApi.Infrastructure.Persistence;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Services
{
    public interface ICouponService
    {
        Task<IReadOnlyList<CouponDto>> ListAsync(Guid branchId, CancellationToken ct);
        Task<Guid> CreateAsync(Guid branchId, CouponCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid couponId, CouponUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid couponId, CancellationToken ct);
        Task ToggleAsync(Guid branchId, bool active, CancellationToken ct);
    }

    public sealed class CouponService : ICouponService
    {
        private readonly AppDbContext _db;
        public CouponService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<CouponDto>> ListAsync(Guid branchId, CancellationToken ct)
{
    return await _db.Coupons
        .AsNoTracking()
        .Where(c => c.BranchId == branchId)
        // Orden: activos primero, luego por StartDate desc (nulls al final), luego por Code
        .OrderByDescending(c => c.Active)
        .ThenByDescending(c => c.StartDate ?? DateTimeOffset.MinValue)
        .ThenBy(c => c.Code)
        .Select(c => new CouponDto
        {
            Id = c.Id,
            BranchId = c.BranchId,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            Type = c.Type,
            Value = c.Value,
            AppliesTo = c.AppliesTo,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            Active = c.Active,
            MaxUsesPerUser = c.MaxUsesPerUser,
            MaxTotalUses = c.MaxTotalUses
        })
        .ToListAsync(ct);
}


        public async Task<Guid> CreateAsync(Guid branchId, CouponCreateRequest req, CancellationToken ct)
        {
            ValidateCoupon(req.Type, req.Value, req.AppliesTo, req.StartDate, req.EndDate);

            var code = req.Code.Trim().ToUpperInvariant();

            var exists = await _db.Coupons
                .AnyAsync(c => c.BranchId == branchId && c.Code == code, ct);
            if (exists) throw new InvalidOperationException("El código ya existe en esta sucursal.");

            var entity = new Coupon
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Code = code,
                Name = req.Name,
                Description = req.Description,
                Type = req.Type,
                Value = req.Value,
                AppliesTo = req.AppliesTo,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Active = req.Active,
                MaxUsesPerUser = Math.Max(0, req.MaxUsesPerUser),
                MaxTotalUses = req.MaxTotalUses
            };

            _db.Coupons.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Guid couponId, CouponUpdateRequest req, CancellationToken ct)
        {
            var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == couponId, ct)
                ?? throw new KeyNotFoundException("Cupón no encontrado.");

            var type = req.Type ?? c.Type;
            var value = req.Value ?? c.Value;
            var applies = req.AppliesTo ?? c.AppliesTo;
            var start = req.StartDate ?? c.StartDate;
            var end = req.EndDate ?? c.EndDate;

            ValidateCoupon(type, value, applies, start, end);

            if (req.Name is not null) c.Name = req.Name;
            if (req.Description is not null) c.Description = req.Description;
            if (req.Type is not null) c.Type = req.Type;
            if (req.Value.HasValue) c.Value = req.Value.Value;
            if (req.AppliesTo is not null) c.AppliesTo = req.AppliesTo;
            if (req.StartDate.HasValue) c.StartDate = req.StartDate.Value;
            if (req.EndDate.HasValue) c.EndDate = req.EndDate.Value;
            if (req.Active.HasValue) c.Active = req.Active.Value;
            if (req.MaxUsesPerUser.HasValue) c.MaxUsesPerUser = Math.Max(0, req.MaxUsesPerUser.Value);
            if (req.MaxTotalUses.HasValue) c.MaxTotalUses = req.MaxTotalUses.Value;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid couponId, CancellationToken ct)
        {
            var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == couponId, ct)
                ?? throw new KeyNotFoundException("Cupón no encontrado.");

            // “Borrado” seguro: desactivar
            c.Active = false;
            c.EndDate ??= DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task ToggleAsync(Guid branchId, bool active, CancellationToken ct)
        {
            var list = await _db.Coupons.Where(c => c.BranchId == branchId).ToListAsync(ct);
            foreach (var c in list) c.Active = active;
            await _db.SaveChangesAsync(ct);
        }

        // ===== Helpers =====
        private static void ValidateCoupon(string type, decimal value, string appliesTo,
            DateTimeOffset? startDate, DateTimeOffset? endDate)
        {
            if (type is not ("percent" or "fixed"))
                throw new InvalidOperationException("Tipo inválido (percent|fixed).");

            if (type == "percent" && (value <= 0 || value > 100))
                throw new InvalidOperationException("El porcentaje debe estar entre 0 y 100.");
            if (type == "fixed" && value < 0)
                throw new InvalidOperationException("El valor fijo no puede ser negativo.");

            if (appliesTo is not ("all" or "specific_user" or "first_booking"))
                throw new InvalidOperationException("applies_to inválido.");

            if (startDate.HasValue && endDate.HasValue && endDate < startDate)
                throw new InvalidOperationException("Rango de fechas inválido (EndDate < StartDate).");
        }
    }
}
