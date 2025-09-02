using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Services;
using WebApi.Infrastructure.Persistence;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Services
{
    public interface IServiceCatalogService
    {
        // Services
        Task<IReadOnlyList<ServiceDto>> ListAsync(Guid branchId, CancellationToken ct);
        Task<Guid> CreateAsync(Guid branchId, ServiceCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid serviceId, ServiceUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid serviceId, CancellationToken ct);

        // Options
        Task<Guid> AddOptionAsync(Guid serviceId, ServiceOptionCreateRequest req, CancellationToken ct);
        Task UpdateOptionAsync(Guid optionId, ServiceOptionUpdateRequest req, CancellationToken ct);
        Task DeleteOptionAsync(Guid optionId, CancellationToken ct);

        // Extras catalog
        Task<IReadOnlyList<ExtraDto>> ExtrasListAsync(Guid branchId, CancellationToken ct);
        Task<Guid> ExtrasCreateAsync(Guid branchId, ExtraCreateRequest req, CancellationToken ct);
        Task ExtrasUpdateAsync(Guid id, ExtraUpdateRequest req, CancellationToken ct);
        Task ExtrasDeleteAsync(Guid id, CancellationToken ct);

        // Service↔Extra link
        Task LinkExtraAsync(Guid serviceId, Guid extraId, CancellationToken ct);
        Task UnlinkExtraAsync(Guid serviceId, Guid extraId, CancellationToken ct);
    }

    public sealed class ServiceCatalogService : IServiceCatalogService
    {
        private readonly AppDbContext _db;
        public ServiceCatalogService(AppDbContext db) => _db = db;

        // ===== Services =====
        public async Task<IReadOnlyList<ServiceDto>> ListAsync(Guid branchId, CancellationToken ct)
        {
            // devolvemos servicios con opciones y extras (solo ids/nombres de extras)
            var items = await _db.Services
                .AsNoTracking()
                .Include(s => s.Options)
                .Include(s => s.ServiceExtras).ThenInclude(se => se.Extra)
                .Where(s => s.BranchId == branchId)
                .OrderBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    BranchId = s.BranchId,
                    CategoryId = s.CategoryId,
                    Name = s.Name,
                    Description = s.Description,
                    BasePrice = s.BasePrice,
                    DurationMin = s.DurationMin,
                    BufferBefore = s.BufferBefore,
                    BufferAfter = s.BufferAfter,
                    RequiresResource = s.RequiresResource,
                    Active = s.Active,
                    ImageUrl = s.ImageUrl,
                    Options = s.Options
                        .OrderBy(o => o.Name)
                        .Select(o => new ServiceDto.ServiceOptionItem
                        {
                            Id = o.Id, Name = o.Name, PriceDelta = o.PriceDelta, DurationDelta = o.DurationDelta, ImageUrl = o.ImageUrl
                        }).ToList(),
                    Extras = s.ServiceExtras
                        .OrderBy(e => e.Extra.Name)
                        .Select(e => new ServiceDto.ServiceExtraItem
                        {
                            Id = e.ExtraId, Name = e.Extra.Name, Price = e.Extra.Price, DurationMin = e.Extra.DurationMin, Active = e.Extra.Active
                        }).ToList()
                })
                .ToListAsync(ct);

            return items;
        }

        public async Task<Guid> CreateAsync(Guid branchId, ServiceCreateRequest req, CancellationToken ct)
        {
            ValidateService(req.BasePrice, req.DurationMin, req.BufferBefore, req.BufferAfter);

            if (req.CategoryId.HasValue)
            {
                var catExists = await _db.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId.Value && c.BranchId == branchId, ct);
                if (!catExists) throw new InvalidOperationException("Categoría inválida para esta sucursal.");
            }

            var entity = new Service
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                CategoryId = req.CategoryId,
                Name = req.Name.Trim(),
                Description = req.Description,
                BasePrice = req.BasePrice,
                DurationMin = req.DurationMin,
                BufferBefore = req.BufferBefore,
                BufferAfter = req.BufferAfter,
                RequiresResource = req.RequiresResource,
                Active = req.Active,
                ImageUrl = req.ImageUrl
            };
            _db.Services.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Guid serviceId, ServiceUpdateRequest req, CancellationToken ct)
        {
            var s = await _db.Services.FirstOrDefaultAsync(x => x.Id == serviceId, ct)
                ?? throw new KeyNotFoundException("Servicio no encontrado.");

            if (req.BasePrice.HasValue || req.DurationMin.HasValue || req.BufferBefore.HasValue || req.BufferAfter.HasValue)
                ValidateService(req.BasePrice ?? s.BasePrice, req.DurationMin ?? s.DurationMin, req.BufferBefore ?? s.BufferBefore, req.BufferAfter ?? s.BufferAfter);

            if (req.CategoryId.HasValue)
            {
                var valid = await _db.ServiceCategories.AnyAsync(c => c.Id == req.CategoryId.Value && c.BranchId == s.BranchId, ct);
                if (!valid) throw new InvalidOperationException("Categoría inválida para esta sucursal.");
                s.CategoryId = req.CategoryId;
            }

            if (req.Name is not null) s.Name = req.Name.Trim();
            if (req.Description is not null) s.Description = req.Description;
            if (req.BasePrice.HasValue) s.BasePrice = req.BasePrice.Value;
            if (req.DurationMin.HasValue) s.DurationMin = req.DurationMin.Value;
            if (req.BufferBefore.HasValue) s.BufferBefore = req.BufferBefore.Value;
            if (req.BufferAfter.HasValue) s.BufferAfter = req.BufferAfter.Value;
            if (req.RequiresResource.HasValue) s.RequiresResource = req.RequiresResource.Value;
            if (req.Active.HasValue) s.Active = req.Active.Value;
            if (req.ImageUrl is not null) s.ImageUrl = req.ImageUrl;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid serviceId, CancellationToken ct)
        {
            var s = await _db.Services.FirstOrDefaultAsync(x => x.Id == serviceId, ct)
                ?? throw new KeyNotFoundException("Servicio no encontrado.");

            // Soft: desactivar
            s.Active = false;
            await _db.SaveChangesAsync(ct);
        }

        // ===== Options =====
        public async Task<Guid> AddOptionAsync(Guid serviceId, ServiceOptionCreateRequest req, CancellationToken ct)
        {
            if (req.PriceDelta < 0) throw new InvalidOperationException("PriceDelta no puede ser negativo.");
            // DurationDelta puede ser negativo/positivo (acorta o alarga)

            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct)
                ?? throw new KeyNotFoundException("Servicio no encontrado.");

            var dup = await _db.ServiceOptions.AnyAsync(o => o.ServiceId == serviceId && o.Name == req.Name, ct);
            if (dup) throw new InvalidOperationException("Ya existe una opción con ese nombre.");

            var opt = new ServiceOption
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                Name = req.Name.Trim(),
                PriceDelta = req.PriceDelta,
                DurationDelta = req.DurationDelta,
                ImageUrl = req.ImageUrl
            };
            _db.ServiceOptions.Add(opt);
            await _db.SaveChangesAsync(ct);
            return opt.Id;
        }

        public async Task UpdateOptionAsync(Guid optionId, ServiceOptionUpdateRequest req, CancellationToken ct)
        {
            var opt = await _db.ServiceOptions.FirstOrDefaultAsync(o => o.Id == optionId, ct)
                ?? throw new KeyNotFoundException("Opción no encontrada.");

            if (req.Name is not null)
            {
                var dup = await _db.ServiceOptions.AnyAsync(o => o.ServiceId == opt.ServiceId && o.Name == req.Name && o.Id != optionId, ct);
                if (dup) throw new InvalidOperationException("Ya existe una opción con ese nombre.");
                opt.Name = req.Name.Trim();
            }
            if (req.PriceDelta.HasValue)
            {
                if (req.PriceDelta.Value < 0) throw new InvalidOperationException("PriceDelta no puede ser negativo.");
                opt.PriceDelta = req.PriceDelta.Value;
            }
            if (req.DurationDelta.HasValue) opt.DurationDelta = req.DurationDelta.Value;
            if (req.ImageUrl is not null) opt.ImageUrl = req.ImageUrl;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteOptionAsync(Guid optionId, CancellationToken ct)
        {
            var opt = await _db.ServiceOptions.FirstOrDefaultAsync(o => o.Id == optionId, ct)
                ?? throw new KeyNotFoundException("Opción no encontrada.");

            _db.ServiceOptions.Remove(opt);
            await _db.SaveChangesAsync(ct);
        }

        // ===== Extras (catálogo por branch) =====
        public async Task<IReadOnlyList<ExtraDto>> ExtrasListAsync(Guid branchId, CancellationToken ct)
        {
            return await _db.Extras
                .AsNoTracking()
                .Where(e => e.BranchId == branchId)
                .OrderBy(e => e.Name)
                .Select(e => new ExtraDto
                {
                    Id = e.Id, BranchId = e.BranchId, Name = e.Name,
                    Description = e.Description, Price = e.Price, DurationMin = e.DurationMin,
                    Active = e.Active, ImageUrl = e.ImageUrl
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> ExtrasCreateAsync(Guid branchId, ExtraCreateRequest req, CancellationToken ct)
        {
            var e = new Extra
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = req.Name.Trim(),
                Description = req.Description,
                Price = req.Price,
                DurationMin = req.DurationMin,
                Active = req.Active,
                ImageUrl = req.ImageUrl
            };
            _db.Extras.Add(e);
            await _db.SaveChangesAsync(ct);
            return e.Id;
        }

        public async Task ExtrasUpdateAsync(Guid id, ExtraUpdateRequest req, CancellationToken ct)
        {
            var e = await _db.Extras.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Extra no encontrado.");

            if (req.Name is not null) e.Name = req.Name.Trim();
            if (req.Description is not null) e.Description = req.Description;
            if (req.Price.HasValue) e.Price = req.Price.Value;
            if (req.DurationMin.HasValue) e.DurationMin = req.DurationMin.Value;
            if (req.Active.HasValue) e.Active = req.Active.Value;
            if (req.ImageUrl is not null) e.ImageUrl = req.ImageUrl;

            await _db.SaveChangesAsync(ct);
        }

        public async Task ExtrasDeleteAsync(Guid id, CancellationToken ct)
        {
            var e = await _db.Extras.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Extra no encontrado.");

            // Soft: desactivar (para no romper histórico)
            e.Active = false;
            await _db.SaveChangesAsync(ct);
        }

        // ===== Vinculación Service↔Extra =====
        public async Task LinkExtraAsync(Guid serviceId, Guid extraId, CancellationToken ct)
        {
            var svc = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct)
                ?? throw new KeyNotFoundException("Servicio no encontrado.");

            var extra = await _db.Extras.FirstOrDefaultAsync(e => e.Id == extraId && e.BranchId == svc.BranchId, ct)
                ?? throw new InvalidOperationException("El extra no pertenece a la misma sucursal.");

            var exists = await _db.ServiceExtras.AnyAsync(se => se.ServiceId == serviceId && se.ExtraId == extraId, ct);
            if (exists) return;

            _db.ServiceExtras.Add(new ServiceExtra { ServiceId = serviceId, ExtraId = extraId });
            await _db.SaveChangesAsync(ct);
        }

        public async Task UnlinkExtraAsync(Guid serviceId, Guid extraId, CancellationToken ct)
        {
            var link = await _db.ServiceExtras.FirstOrDefaultAsync(se => se.ServiceId == serviceId && se.ExtraId == extraId, ct);
            if (link is null) return;

            _db.ServiceExtras.Remove(link);
            await _db.SaveChangesAsync(ct);
        }

        // ===== Helpers =====
        private static void ValidateService(decimal basePrice, int durationMin, int bufferBefore, int bufferAfter)
        {
            if (basePrice < 0) throw new InvalidOperationException("BasePrice no puede ser negativo.");
            if (durationMin <= 0) throw new InvalidOperationException("DurationMin debe ser > 0.");
            if (bufferBefore < 0 || bufferAfter < 0) throw new InvalidOperationException("Buffers no pueden ser negativos.");
        }
    }
}
