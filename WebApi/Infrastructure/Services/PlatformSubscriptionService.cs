// Infrastructure/Services/Platform/PlatformSubscriptionService.cs
using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Platform.Subscriptions;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services.Platform
{
    public interface IPlatformSubscriptionService
    {
        // Planes
        Task<IReadOnlyList<PlanDto>> ListPlansAsync(CancellationToken ct);
        Task<Guid> CreatePlanAsync(PlanCreateRequest req, CancellationToken ct);
        Task UpdatePlanAsync(Guid planId, PlanUpdateRequest req, CancellationToken ct);
        Task DeletePlanAsync(Guid planId, CancellationToken ct);

        // Suscripción por negocio
        Task<BusinessSubscriptionDto> GetBusinessSubscriptionAsync(Guid businessId, CancellationToken ct);
        Task<Guid> AssignSubscriptionAsync(Guid businessId, AssignSubscriptionRequest req, CancellationToken ct);
        Task UpdateBusinessSubscriptionAsync(Guid businessId, AssignSubscriptionRequest req, CancellationToken ct);

        // Invoices (usages) del negocio
        Task<IReadOnlyList<PlatformInvoiceDto>> ListBusinessInvoicesAsync(Guid businessId, CancellationToken ct);
    }

    public sealed class PlatformSubscriptionService : IPlatformSubscriptionService
    {
        private readonly AppDbContext _db;
        public PlatformSubscriptionService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<PlanDto>> ListPlansAsync(CancellationToken ct)
        {
            return await _db.SubscriptionPlans.AsNoTracking()
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    PricePerStaff = p.PricePerStaff,
                    PricePerBranch = p.PricePerBranch,
                    Active = p.Active
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> CreatePlanAsync(PlanCreateRequest req, CancellationToken ct)
        {
            var p = new WebApi.Domain.Entities.SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                BasePrice = req.BasePrice,
                PricePerStaff = req.PricePerStaff,
                PricePerBranch = req.PricePerBranch,
                Active = req.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.SubscriptionPlans.Add(p);
            await _db.SaveChangesAsync(ct);
            return p.Id;
        }

        public async Task UpdatePlanAsync(Guid planId, PlanUpdateRequest req, CancellationToken ct)
        {
            var p = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId, ct)
                ?? throw new KeyNotFoundException("Plan no encontrado.");
            p.Name = req.Name;
            p.BasePrice = req.BasePrice;
            p.PricePerStaff = req.PricePerStaff;
            p.PricePerBranch = req.PricePerBranch;
            p.Active = req.Active;
            p.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeletePlanAsync(Guid planId, CancellationToken ct)
        {
            var p = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId, ct)
                ?? throw new KeyNotFoundException("Plan no encontrado.");
            _db.SubscriptionPlans.Remove(p);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<BusinessSubscriptionDto> GetBusinessSubscriptionAsync(Guid businessId, CancellationToken ct)
        {
            var s = await _db.BusinessSubscriptions.Include(x => x.Plan)
                .FirstOrDefaultAsync(x => x.BusinessId == businessId, ct)
                ?? throw new KeyNotFoundException("Suscripción no encontrada.");

            return new BusinessSubscriptionDto
            {
                SubscriptionId = s.Id,
                PlanId = s.PlanId,
                PlanName = s.Plan.Name,
                Status = s.Status,
                BillingAnchorDay = s.BillingAnchorDay,
                StartedAt = s.StartedAt,
                CancelAt = s.CancelAt
            };
        }

        public async Task<Guid> AssignSubscriptionAsync(Guid businessId, AssignSubscriptionRequest req, CancellationToken ct)
        {
            var exists = await _db.BusinessSubscriptions.FirstOrDefaultAsync(x => x.BusinessId == businessId, ct);
            if (exists is not null) throw new InvalidOperationException("La cuenta ya tiene suscripción.");

            var s = new WebApi.Domain.Entities.BusinessSubscription
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                PlanId = req.PlanId,
                Status = "active",
                BillingAnchorDay = req.BillingAnchorDay ?? DateTime.UtcNow.Day,
                StartedAt = DateTimeOffset.UtcNow
            };
            _db.BusinessSubscriptions.Add(s);
            await _db.SaveChangesAsync(ct);
            return s.Id;
        }

        public async Task UpdateBusinessSubscriptionAsync(Guid businessId, AssignSubscriptionRequest req, CancellationToken ct)
        {
            var s = await _db.BusinessSubscriptions.FirstOrDefaultAsync(x => x.BusinessId == businessId, ct)
                ?? throw new KeyNotFoundException("Suscripción no encontrada.");
            s.PlanId = req.PlanId;
            if (req.BillingAnchorDay.HasValue) s.BillingAnchorDay = req.BillingAnchorDay.Value;
            s.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<PlatformInvoiceDto>> ListBusinessInvoicesAsync(Guid businessId, CancellationToken ct)
        {
            var items = await _db.SubscriptionUsages.AsNoTracking()
                .Where(u => u.BusinessId == businessId)
                .OrderByDescending(u => u.Year).ThenByDescending(u => u.Month)
                .Select(u => new PlatformInvoiceDto
                {
                    Id = u.Id,
                    BusinessId = u.BusinessId,
                    Year = u.Year,
                    Month = u.Month,
                    Total = u.Total,
                    Status = u.Status,
                    ProviderInvoiceId = u.ProviderInvoiceId,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(ct);

            return items;
        }
    }
}
