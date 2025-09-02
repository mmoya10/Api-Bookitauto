using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Resources;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IResourceService
    {
        Task<IReadOnlyList<ResourceDto>> ListAsync(Guid branchId, CancellationToken ct);
        Task<Guid> CreateAsync(Guid branchId, ResourceCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid id, ResourceUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
    }

    public sealed class ResourceService : IResourceService
    {
        private readonly AppDbContext _db;
        public ResourceService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<ResourceDto>> ListAsync(Guid branchId, CancellationToken ct)
        {
            return await _db.Resources
                .AsNoTracking()
                .Where(r => r.BranchId == branchId)
                .OrderBy(r => r.Name)
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    BranchId = r.BranchId,
                    Name = r.Name,
                    Description = r.Description,
                    TotalQuantity = r.TotalQuantity
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> CreateAsync(Guid branchId, ResourceCreateRequest req, CancellationToken ct)
        {
            Validate(req.Name, req.TotalQuantity);

            var dup = await _db.Resources
                .AnyAsync(r => r.BranchId == branchId && r.Name == req.Name, ct);
            if (dup) throw new InvalidOperationException("Ya existe un recurso con ese nombre en la sucursal.");

            var entity = new Domain.Entities.Resource
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = req.Name.Trim(),
                Description = req.Description,
                TotalQuantity = req.TotalQuantity
            };

            _db.Resources.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Guid id, ResourceUpdateRequest req, CancellationToken ct)
        {
            var r = await _db.Resources.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Recurso no encontrado.");

            if (req.Name is not null)
            {
                var dup = await _db.Resources
                    .AnyAsync(x => x.BranchId == r.BranchId && x.Name == req.Name && x.Id != id, ct);
                if (dup) throw new InvalidOperationException("Ya existe un recurso con ese nombre en la sucursal.");
                r.Name = req.Name.Trim();
            }

            if (req.TotalQuantity.HasValue)
            {
                if (req.TotalQuantity.Value <= 0) throw new InvalidOperationException("TotalQuantity debe ser > 0.");
                r.TotalQuantity = req.TotalQuantity.Value;
            }

            if (req.Description is not null)
                r.Description = req.Description;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var r = await _db.Resources.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Recurso no encontrado.");

            // Evitar borrar si está vinculado a servicios
            var linked = await _db.ServiceResources.AnyAsync(sr => sr.ResourceId == id, ct);
            if (linked) throw new InvalidOperationException("No se puede eliminar: el recurso está asignado a servicios.");

            _db.Resources.Remove(r);
            await _db.SaveChangesAsync(ct);
        }

        private static void Validate(string name, int totalQty)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("El nombre es obligatorio.");
            if (totalQty <= 0) throw new InvalidOperationException("TotalQuantity debe ser > 0.");
        }
    }
}
