using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.DTOs.Products;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IProductService
    {
        Task<(IReadOnlyList<ProductListItemDto> items, int total)> ListAsync(
            Guid branchId, string? query, Guid? categoryId, bool? active, 
            string? sort, bool desc, int page, int size, CancellationToken ct);

        Task<Guid> CreateAsync(Guid branchId, ProductCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid productId, ProductUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid productId, CancellationToken ct);
    }

    public sealed class ProductService : IProductService
    {
        private readonly AppDbContext _db;

        public ProductService(AppDbContext db) => _db = db;

        public async Task<(IReadOnlyList<ProductListItemDto> items, int total)> ListAsync(
            Guid branchId, string? query, Guid? categoryId, bool? active, 
            string? sort, bool desc, int page, int size, CancellationToken ct)
        {
            var q = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.BranchId == branchId && p.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var qnorm = query.Trim();
                q = q.Where(p =>
                    p.Sku.Contains(qnorm) ||
                    p.Name.Contains(qnorm) ||
                    (p.Description != null && p.Description.Contains(qnorm)));
            }

            if (categoryId is Guid catId)
                q = q.Where(p => p.CategoryId == catId);

            if (active is bool a)
                q = q.Where(p => p.Active == a);

            q = (sort?.ToLowerInvariant(), desc) switch
            {
                ("name", true)    => q.OrderByDescending(p => p.Name),
                ("name", false)   => q.OrderBy(p => p.Name),
                ("sku", true)     => q.OrderByDescending(p => p.Sku),
                ("sku", false)    => q.OrderBy(p => p.Sku),
                ("price", true)   => q.OrderByDescending(p => p.Price),
                ("price", false)  => q.OrderBy(p => p.Price),
                _                 => q.OrderByDescending(p => p.CreatedAt)
            };

            var total = await q.CountAsync(ct);
            var items = await q.Skip((page - 1) * size).Take(size)
                .Select(p => new ProductListItemDto
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    OfferPrice = p.OfferPrice,
                    Active = p.Active,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<Guid> CreateAsync(Guid branchId, ProductCreateRequest req, CancellationToken ct)
        {
            await ValidateCategoryAsync(branchId, req.CategoryId, ct);
            ValidatePricing(req.Price, req.OfferPrice, req.OfferStart, req.OfferEnd);

            var existsSku = await _db.Products
                .AnyAsync(p => p.BranchId == branchId && p.Sku == req.Sku && p.DeletedAt == null, ct);

            if (existsSku)
                throw new InvalidOperationException("SKU ya existe en esta sucursal.");

            var entity = new Product
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                CategoryId = req.CategoryId,
                Sku = req.Sku.Trim(),
                Name = req.Name.Trim(),
                Description = req.Description,
                Price = req.Price,
                OfferPrice = req.OfferPrice,
                OfferStart = req.OfferStart,
                OfferEnd = req.OfferEnd,
                Active = req.Active,
                ImageUrl = req.ImageUrl,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Products.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Guid productId, ProductUpdateRequest req, CancellationToken ct)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.DeletedAt == null, ct);
            if (p is null) throw new KeyNotFoundException("Producto no encontrado.");

            if (req.CategoryId.HasValue)
                await ValidateCategoryAsync(p.BranchId, req.CategoryId, ct);

            var newPrice = req.Price ?? p.Price;
            var newOfferPrice = req.OfferPrice ?? p.OfferPrice;
            var newOfferStart = req.OfferStart ?? p.OfferStart;
            var newOfferEnd = req.OfferEnd ?? p.OfferEnd;

            ValidatePricing(newPrice, newOfferPrice, newOfferStart, newOfferEnd);

            if (!string.IsNullOrWhiteSpace(req.Sku) && !string.Equals(req.Sku, p.Sku, StringComparison.Ordinal))
            {
                var exists = await _db.Products
                    .AnyAsync(x => x.BranchId == p.BranchId && x.Sku == req.Sku && x.Id != p.Id && x.DeletedAt == null, ct);
                if (exists) throw new InvalidOperationException("SKU ya existe en esta sucursal.");
                p.Sku = req.Sku.Trim();
            }

            if (!string.IsNullOrWhiteSpace(req.Name)) p.Name = req.Name.Trim();
            if (req.Description is not null) p.Description = req.Description;
            if (req.Price.HasValue) p.Price = req.Price.Value;
            if (req.OfferPrice.HasValue) p.OfferPrice = req.OfferPrice;
            if (req.OfferStart.HasValue) p.OfferStart = req.OfferStart;
            if (req.OfferEnd.HasValue) p.OfferEnd = req.OfferEnd;
            if (req.Active.HasValue) p.Active = req.Active.Value;
            if (req.ImageUrl is not null) p.ImageUrl = req.ImageUrl;

            p.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid productId, CancellationToken ct)
        {
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.DeletedAt == null, ct);
            if (p is null) return;

            // Soft delete
            p.Active = false;
            p.DeletedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        // ===== helpers =====

        private async Task ValidateCategoryAsync(Guid branchId, Guid? categoryId, CancellationToken ct)
        {
            if (!categoryId.HasValue) return;
            var exists = await _db.ProductCategories.AnyAsync(c => c.Id == categoryId && c.BranchId == branchId, ct);
            if (!exists) throw new InvalidOperationException("La categoría no pertenece a esta sucursal.");
        }

        private static void ValidatePricing(decimal price, decimal? offerPrice, DateTimeOffset? start, DateTimeOffset? end)
        {
            if (price < 0) throw new InvalidOperationException("El precio no puede ser negativo.");
            if (offerPrice.HasValue)
            {
                if (offerPrice.Value < 0) throw new InvalidOperationException("La oferta no puede ser negativa.");
                if (offerPrice.Value > price) throw new InvalidOperationException("La oferta no puede ser mayor que el precio.");
                if (start is null || end is null) throw new InvalidOperationException("OfferStart y OfferEnd son obligatorios si hay OfferPrice.");
                if (end <= start) throw new InvalidOperationException("OfferEnd debe ser mayor que OfferStart.");
            }
        }
    }
}
