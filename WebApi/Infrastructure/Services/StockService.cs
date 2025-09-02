using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.DTOs.Stock;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IStockService
    {
        Task<(IReadOnlyList<StockItemDto> items, int total)> GetStockAsync(
            Guid branchId, string? query, bool? belowMinOnly, int page, int size, CancellationToken ct);

        Task<(IReadOnlyList<StockMovementDto> items, int total)> GetMovementsAsync(
            Guid branchId, Guid? productId, string? type, DateTimeOffset? from, DateTimeOffset? to,
            int page, int size, CancellationToken ct);

        Task<Guid> CreateMovementAsync(Guid branchId, StockMovementCreateRequest req, CancellationToken ct);

        Task DeleteMovementAsync(Guid movementId, CancellationToken ct);
    }

    public sealed class StockService : IStockService
    {
        private readonly AppDbContext _db;
        public StockService(AppDbContext db) => _db = db;

        // ===== STOCK LIST =====
        public async Task<(IReadOnlyList<StockItemDto> items, int total)> GetStockAsync(
            Guid branchId, string? query, bool? belowMinOnly, int page, int size, CancellationToken ct)
        {
            var q = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Stock)
                .Where(p => p.BranchId == branchId && p.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var qn = query.Trim();
                q = q.Where(p =>
                    p.Sku.Contains(qn) ||
                    p.Name.Contains(qn) ||
                    (p.Description != null && p.Description.Contains(qn)));
            }

            if (belowMinOnly is true)
                q = q.Where(p => p.Stock != null && p.Stock.CurrentStock < p.Stock.MinStock);

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderBy(p => p.Name)
                .Skip((page - 1) * size).Take(size)
                .Select(p => new StockItemDto
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    MinStock = p.Stock != null ? p.Stock.MinStock : 0,
                    CurrentStock = p.Stock != null ? p.Stock.CurrentStock : 0,
                    Active = p.Active,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync(ct);

            return (items, total);
        }

        // ===== MOVEMENTS LIST =====
        public async Task<(IReadOnlyList<StockMovementDto> items, int total)> GetMovementsAsync(
            Guid branchId, Guid? productId, string? type, DateTimeOffset? from, DateTimeOffset? to,
            int page, int size, CancellationToken ct)
        {
            var q = _db.StockMovements
                .AsNoTracking()
                .Include(m => m.Product)
                .Where(m => m.Product.BranchId == branchId && m.Product.DeletedAt == null);

            if (productId.HasValue)
                q = q.Where(m => m.ProductId == productId.Value);

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(m => m.Type == type);

            if (from.HasValue) q = q.Where(m => m.CreatedAt >= from.Value);
            if (to.HasValue)   q = q.Where(m => m.CreatedAt <= to.Value);

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * size).Take(size)
                .Select(m => new StockMovementDto
                {
                    Id = m.Id,
                    ProductId = m.ProductId,
                    Sku = m.Product.Sku,
                    ProductName = m.Product.Name,
                    Quantity = m.Quantity,
                    Type = m.Type,
                    TotalPrice = m.TotalPrice,
                    Notes = m.Notes,
                    ReferenceType = m.ReferenceType,
                    ReferenceId = m.ReferenceId,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);

            return (items, total);
        }

        // ===== CREATE MOVEMENT =====
        public async Task<Guid> CreateMovementAsync(Guid branchId, StockMovementCreateRequest req, CancellationToken ct)
        {
            if (req.Quantity == 0) throw new InvalidOperationException("Quantity no puede ser 0.");
            if (req.Type is not ("purchase" or "adjustment" or "sale"))
                throw new InvalidOperationException("Tipo de movimiento inválido.");

            // Validar producto
            var product = await _db.Products
                .Include(p => p.Stock)
                .FirstOrDefaultAsync(p => p.Id == req.ProductId && p.BranchId == branchId && p.DeletedAt == null, ct)
                ?? throw new KeyNotFoundException("Producto no encontrado en esta sucursal.");

            // Asegurar registro de stock
            var stock = product.Stock ?? new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                MinStock = 0,
                CurrentStock = 0
            };
            if (product.Stock is null) _db.Stocks.Add(stock);

            // Regla: el resultado no puede ser negativo
            var newQty = stock.CurrentStock + req.Quantity;
            if (newQty < 0)
                throw new InvalidOperationException("El stock resultante no puede ser negativo.");

            // Si viene referencia sale/expense, forzamos coherencia
            if (req.ReferenceType is not null)
            {
                if (req.ReferenceType is not ("sale" or "expense"))
                    throw new InvalidOperationException("ReferenceType inválido (sale|expense).");
                if (!req.ReferenceId.HasValue)
                    throw new InvalidOperationException("ReferenceId requerido cuando hay ReferenceType.");
            }

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = req.Quantity,
                Type = req.Type,
                TotalPrice = req.TotalPrice,
                Notes = req.Notes,
                ReferenceType = req.ReferenceType,
                ReferenceId = req.ReferenceId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Aplicar
            stock.CurrentStock = newQty;
            _db.StockMovements.Add(movement);

            await _db.SaveChangesAsync(ct);
            return movement.Id;
        }

        // ===== DELETE MOVEMENT =====
        public async Task DeleteMovementAsync(Guid movementId, CancellationToken ct)
        {
            var mv = await _db.StockMovements
                .Include(m => m.Product).ThenInclude(p => p.Stock)
                .FirstOrDefaultAsync(m => m.Id == movementId, ct)
                ?? throw new KeyNotFoundException("Movimiento no encontrado.");

            // No permitir borrar movimientos ligados a ventas/gastos
            if (!string.IsNullOrEmpty(mv.ReferenceType))
                throw new InvalidOperationException("No se puede eliminar un movimiento referenciado.");

            var stock = mv.Product.Stock ?? throw new InvalidOperationException("Stock no encontrado para el producto.");

            // Revertir efecto del movimiento
            var reverted = stock.CurrentStock - mv.Quantity; // restamos lo que sumamos (o sumamos lo que restamos)
            if (reverted < 0)
                throw new InvalidOperationException("No se puede eliminar; el stock quedaría negativo.");

            stock.CurrentStock = reverted;
            _db.StockMovements.Remove(mv);
            await _db.SaveChangesAsync(ct);
        }
    }
}
