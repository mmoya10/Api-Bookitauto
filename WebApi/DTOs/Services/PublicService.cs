using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Public;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IPublicService
    {
        Task<IReadOnlyList<SearchResultItemDto>> SearchAsync(string? query, string? category, string? city, double? lat, double? lng, CancellationToken ct);
        Task<BusinessPublicDto> GetBusinessAsync(Guid businessId, CancellationToken ct);
        Task<BranchPublicDto> GetBranchAsync(Guid branchId, CancellationToken ct);
    }

    public sealed class PublicService : IPublicService
    {
        private readonly AppDbContext _db;
        public PublicService(AppDbContext db) => _db = db;

        // 🔍 Búsqueda pública básica por nombre/ciudad/categoría (no geodist por simplicidad)
        public async Task<IReadOnlyList<SearchResultItemDto>> SearchAsync(string? query, string? category, string? city, double? lat, double? lng, CancellationToken ct)
        {
            // Base: Businesses activos + Branches activas
            // PublicService.SearchAsync(...)

            var q = _db.Branches.AsNoTracking()
                .Where(br => br.Status == "active" && br.Business.Status == "active")
                .Select(br => new
                {
                    br.Id,
                    br.Name,
                    br.City,
                    br.Province,
                    br.Slug,
                    Business = br.Business,
                    Category = br.Business.Category != null ? br.Business.Category.Name : null,
                    // 🔧 FIX: contar servicios activos por BranchId (subconsulta)
                    ServicesCount = _db.Services.Count(s => s.BranchId == br.Id && s.Active)
                });


            if (!string.IsNullOrWhiteSpace(query))
            {
                var qNorm = query.Trim().ToLower();
                q = q.Where(x =>
                    x.Name.ToLower().Contains(qNorm) ||
                    x.Business.Name.ToLower().Contains(qNorm));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catNorm = category.Trim().ToLower();
                q = q.Where(x => (x.Category ?? "").ToLower().Contains(catNorm));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityNorm = city.Trim().ToLower();
                q = q.Where(x => (x.City ?? "").ToLower().Contains(cityNorm));
            }

            // Nota: lat/lng ignorados por ahora (no hay geolocalización en el modelo). Podemos añadir campos y Haversine luego.

            var items = await q
                .OrderByDescending(x => x.ServicesCount)
                .ThenBy(x => x.Name)
                .Take(50)
                .Select(x => new SearchResultItemDto
                {
                    BusinessId = x.Business.Id,
                    BranchId = x.Id,
                    BusinessName = x.Business.Name,
                    BranchName = x.Name,
                    City = x.City,
                    Province = x.Province,
                    LogoUrl = x.Business.LogoUrl,
                    Category = x.Category,
                    Slug = x.Slug,
                    ServicesCount = x.ServicesCount
                })
                .ToListAsync(ct);


            // Si quieres incluir resultados a nivel "Business" sin branches, se podría añadir otra proyección aquí.

            return items;
        }

        // 🏢 Datos públicos de negocio + branches activas
        public async Task<BusinessPublicDto> GetBusinessAsync(Guid businessId, CancellationToken ct)
        {
            var b = await _db.Businesses
                .AsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == businessId && x.Status == "active", ct)
                ?? throw new KeyNotFoundException("Negocio no encontrado o inactivo.");

            var branches = await _db.Branches.AsNoTracking()
                .Where(br => br.BusinessId == businessId && br.Status == "active")
                .OrderBy(br => br.Name)
                .Select(br => new BusinessPublicDto.BusinessBranchItem
                {
                    Id = br.Id,
                    Name = br.Name,
                    City = br.City,
                    Province = br.Province,
                    Timezone = br.Timezone,
                    Status = br.Status,
                    Slug = br.Slug
                })
                .ToListAsync(ct);

            return new BusinessPublicDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Website = b.Website,
                LogoUrl = b.LogoUrl,
                Category = b.Category?.Name,
                Slug = b.Slug,
                Branches = branches
            };
        }

        // 🏬 Datos públicos de sucursal + catálogo visible (services activos y, si hay site, los enlazados)
        public async Task<BranchPublicDto> GetBranchAsync(Guid branchId, CancellationToken ct)
        {
            var br = await _db.Branches.AsNoTracking()
                .Include(b => b.Business)
                .FirstOrDefaultAsync(b => b.Id == branchId && b.Status == "active" && b.Business.Status == "active", ct)
                ?? throw new KeyNotFoundException("Sucursal no encontrada o inactiva.");

            // Booking site published y visible
            var site = await _db.BookingSites.AsNoTracking()
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.Status == "published" && s.Visible, ct);

            // Servicios visibles: si hay site published+visible, usa sus servicios activos y activos en catálogo; si no, todos los activos del branch
            IQueryable<Domain.Entities.Service> servicesQuery;

            if (site is not null)
            {
                var serviceIdsQ = _db.BookingSiteServices.AsNoTracking()
                    .Where(ss => ss.SiteId == site.Id && ss.Active)
                    .Select(ss => ss.ServiceId);

                servicesQuery = _db.Services.AsNoTracking()
                    .Where(s => s.BranchId == branchId && s.Active && serviceIdsQ.Contains(s.Id));
            }
            else
            {
                servicesQuery = _db.Services.AsNoTracking().Where(s => s.BranchId == branchId && s.Active);
            }

            var services = await servicesQuery
                .OrderBy(s => s.Name)
                .Select(s => new BranchPublicDto.ServiceItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    BasePrice = s.BasePrice,
                    DurationMin = s.DurationMin,
                    ImageUrl = s.ImageUrl,
                    Options = s.Options.Select(o => new BranchPublicDto.OptionItem
                    {
                        Id = o.Id,
                        Name = o.Name,
                        PriceDelta = o.PriceDelta,
                        DurationDelta = o.DurationDelta,
                        ImageUrl = o.ImageUrl
                    }).ToList(),
                    Extras = s.ServiceExtras
                        .Select(se => se.Extra)
                        .Where(e => e.Active)
                        .Select(e => new BranchPublicDto.ExtraItem
                        {
                            Id = e.Id,
                            Name = e.Name,
                            Price = e.Price,
                            DurationMin = e.DurationMin,
                            ImageUrl = e.ImageUrl
                        }).ToList()
                })
                .ToListAsync(ct);

            var schedule = await _db.BranchSchedules.AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .OrderBy(x => x.Weekday).ThenBy(x => x.StartTime)
                .Select(x => new BranchPublicDto.ScheduleItem
                {
                    Weekday = x.Weekday,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                })
                .ToListAsync(ct);

            return new BranchPublicDto
            {
                Id = br.Id,
                BusinessId = br.BusinessId,
                BusinessName = br.Business.Name,
                Name = br.Name,
                Email = br.Email,
                Phone = br.Phone,
                Address = br.Address,
                City = br.City,
                Province = br.Province,
                PostalCode = br.PostalCode,
                Timezone = br.Timezone,
                Slug = br.Slug,
                Services = services,
                Schedule = schedule
            };
        }
    }
}
