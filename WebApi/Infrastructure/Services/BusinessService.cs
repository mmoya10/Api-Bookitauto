using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Business;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IBusinessService
    {
        Task<BusinessDto> GetAsync(Guid businessId, CancellationToken ct);
        Task UpdateAsync(Guid businessId, BusinessUpdateRequest req, CancellationToken ct);
        Task<IReadOnlyList<BusinessBranchListItemDto>> GetBranchesAsync(Guid businessId, CancellationToken ct);
    }

    public sealed class BusinessService : IBusinessService
    {
        private readonly AppDbContext _db;
        public BusinessService(AppDbContext db) => _db = db;

        // Obtiene detalle de negocio
        public async Task<BusinessDto> GetAsync(Guid businessId, CancellationToken ct)
        {
            var b = await _db.Businesses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == businessId, ct)
                ?? throw new KeyNotFoundException("Negocio no encontrado.");

            return new BusinessDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                LegalName = b.LegalName,
                Email = b.Email,
                Phone = b.Phone,
                Website = b.Website,
                CategoryId = b.CategoryId,
                Language = b.Language,
                LogoUrl = b.LogoUrl,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Status = b.Status,
                Slug = b.Slug,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            };
        }

        // Actualiza datos del negocio (valida estado/slug/fechas/categoría)
        public async Task UpdateAsync(Guid businessId, BusinessUpdateRequest req, CancellationToken ct)
        {
            var b = await _db.Businesses.FirstOrDefaultAsync(x => x.Id == businessId, ct)
                ?? throw new KeyNotFoundException("Negocio no encontrado.");

            if (req.Name is not null)        b.Name = req.Name.Trim();
            if (req.Description is not null) b.Description = req.Description;
            if (req.LegalName is not null)   b.LegalName = req.LegalName;
            if (req.Email is not null)       b.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            if (req.Phone is not null)       b.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            if (req.Website is not null)     b.Website = string.IsNullOrWhiteSpace(req.Website) ? null : req.Website.Trim();

            if (req.CategoryId.HasValue)
            {
                var ok = await _db.BusinessCategories.AnyAsync(c => c.Id == req.CategoryId.Value, ct);
                if (!ok) throw new InvalidOperationException("Categoría no válida.");
                b.CategoryId = req.CategoryId.Value;
            }

            if (req.Language is not null)
            {
                if (string.IsNullOrWhiteSpace(req.Language)) throw new InvalidOperationException("Language inválido.");
                b.Language = req.Language.Trim().ToLowerInvariant();
            }

            if (req.LogoUrl is not null) b.LogoUrl = req.LogoUrl;

            if (req.StartDate.HasValue) b.StartDate = req.StartDate.Value;
            if (req.EndDate.HasValue)   b.EndDate   = req.EndDate.Value;
            if (b.StartDate is not null && b.EndDate is not null && b.EndDate < b.StartDate)
                throw new InvalidOperationException("EndDate no puede ser anterior a StartDate.");

            if (req.Status is not null)
            {
                if (req.Status is not ("active" or "inactive" or "suspended"))
                    throw new InvalidOperationException("Status inválido (active|inactive|suspended).");
                b.Status = req.Status;
            }

            if (req.Slug is not null)
            {
                var newSlug = string.IsNullOrWhiteSpace(req.Slug) ? null : req.Slug.Trim();
                if (newSlug is not null)
                {
                    var dup = await _db.Businesses.AnyAsync(x => x.Slug == newSlug && x.Id != b.Id, ct);
                    if (dup) throw new InvalidOperationException("Slug ya existe en otro negocio.");
                }
                b.Slug = newSlug;
            }

            b.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // Lista sucursales del negocio
        public async Task<IReadOnlyList<BusinessBranchListItemDto>> GetBranchesAsync(Guid businessId, CancellationToken ct)
        {
            var exists = await _db.Businesses.AnyAsync(x => x.Id == businessId, ct);
            if (!exists) throw new KeyNotFoundException("Negocio no encontrado.");

            return await _db.Branches.AsNoTracking()
                .Where(br => br.BusinessId == businessId)
                .OrderBy(br => br.Name)
                .Select(br => new BusinessBranchListItemDto
                {
                    Id = br.Id,
                    Name = br.Name,
                    Email = br.Email,
                    Phone = br.Phone,
                    City = br.City,
                    Province = br.Province,
                    Status = br.Status,
                    Slug = br.Slug,
                    Timezone = br.Timezone,
                    CreatedAt = br.CreatedAt
                })
                .ToListAsync(ct);
        }
    }
}
