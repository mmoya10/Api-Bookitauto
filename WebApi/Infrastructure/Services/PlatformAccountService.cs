// Infrastructure/Services/Platform/PlatformAccountService.cs
using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Platform.Accounts;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services.Platform
{
    public interface IPlatformAccountService
    {
        Task<IReadOnlyList<AccountListItemDto>> ListAsync(string? query, CancellationToken ct);
        Task<IReadOnlyList<StaffListItemDto>> StaffByBusinessAsync(Guid businessId, CancellationToken ct);
        Task<Guid> CreateAsync(AccountCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid businessId, AccountUpdateRequest req, CancellationToken ct);
        Task CancelOrDeleteAsync(Guid businessId, CancellationToken ct);
    }

    public sealed class PlatformAccountService : IPlatformAccountService
    {
        private readonly AppDbContext _db;
        public PlatformAccountService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<AccountListItemDto>> ListAsync(string? query, CancellationToken ct)
        {
            var q = _db.Businesses.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query))
                q = q.Where(b => b.Name.ToLower().Contains(query.ToLower()) || (b.LegalName ?? "").ToLower().Contains(query.ToLower()));

            var data = await q
                .OrderBy(b => b.CreatedAt)
                .Select(b => new AccountListItemDto
                {
                    BusinessId = b.Id,
                    Name = b.Name,
                    LegalName = b.LegalName,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    Branches = _db.Branches.Count(x => x.BusinessId == b.Id),
                    Staff = _db.Staff.Count(x => x.Branch.BusinessId == b.Id),
                })
                .ToListAsync(ct);

            return data;
        }

        public async Task<IReadOnlyList<StaffListItemDto>> StaffByBusinessAsync(Guid businessId, CancellationToken ct)
        {
            var items = await _db.Staff
                .AsNoTracking()
                .Where(s => s.Branch.BusinessId == businessId)
                .Select(s => new StaffListItemDto
                {
                    Id = s.Id,
                    BranchId = s.BranchId,
                    Username = s.Username,
                    Email = s.Email,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Role = s.Role != null ? s.Role.Name : "Staff",
                    IsManager = s.IsManager,
                    Status = s.Status
                })
                .ToListAsync(ct);

            return items;
        }

        public async Task<Guid> CreateAsync(AccountCreateRequest req, CancellationToken ct)
        {
            var biz = new WebApi.Domain.Entities.Business
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                LegalName = req.LegalName,
                Email = req.Email,
                Phone = req.Phone,
                CategoryId = req.CategoryId,
                Slug = req.Slug,
                Status = "active",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Businesses.Add(biz);
            await _db.SaveChangesAsync(ct);
            return biz.Id;
        }

        public async Task UpdateAsync(Guid businessId, AccountUpdateRequest req, CancellationToken ct)
        {
            var biz = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, ct)
                ?? throw new KeyNotFoundException("Negocio no encontrado.");
            biz.Name = req.Name;
            biz.LegalName = req.LegalName;
            biz.Email = req.Email;
            biz.Phone = req.Phone;
            biz.CategoryId = req.CategoryId;
            biz.Slug = req.Slug ?? biz.Slug;
            if (!string.IsNullOrWhiteSpace(req.Status)) biz.Status = req.Status!;
            biz.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task CancelOrDeleteAsync(Guid businessId, CancellationToken ct)
        {
            // Regla simple: marca como 'inactive' (o elimina si lo prefieres)
            var biz = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, ct)
                ?? throw new KeyNotFoundException("Negocio no encontrado.");
            biz.Status = "inactive";
            biz.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}
