using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Bookings;
using WebApi.Infrastructure.Persistence;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Services
{
    public interface IBookingOpsService
    {
        Task CompleteAsync(Guid bookingId, BookingCompleteRequest req, CancellationToken ct);
    }

    public sealed class BookingOpsService : IBookingOpsService
    {
        private readonly AppDbContext _db;

        public BookingOpsService(AppDbContext db) => _db = db;

        public async Task CompleteAsync(Guid bookingId, BookingCompleteRequest req, CancellationToken ct)
        {
            // Cargar la cita + branch
            var booking = await _db.Bookings
                .Include(b => b.Branch)
                .Include(b => b.Sale)
                .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
                ?? throw new KeyNotFoundException("Reserva no encontrada.");

            if (booking.Status is "cancelled")
                throw new InvalidOperationException("La reserva está cancelada.");

            if (booking.Status is "completed" or "no_show")
                throw new InvalidOperationException("La reserva ya está cerrada.");

            // No show: marcar y salir (sin ventas/movimientos)
            if (string.Equals(req.Outcome, "no_show", StringComparison.OrdinalIgnoreCase))
            {
                booking.Status = "no_show";
                booking.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
                return;
            }

            // ===== Validaciones de COMPLETED =====
            if (!string.Equals(req.Outcome, "completed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Outcome inválido. Use 'completed' o 'no_show'.");

            if (req.ServiceTotal < 0) throw new InvalidOperationException("ServiceTotal no puede ser negativo.");

            foreach (var p in req.Products)
            {
                if (p.Quantity <= 0) throw new InvalidOperationException("Quantity de producto debe ser > 0.");
                if (p.UnitPrice < 0) throw new InvalidOperationException("UnitPrice no puede ser negativo.");
                if (p.Total != p.UnitPrice * p.Quantity) throw new InvalidOperationException("Total de producto inválido.");
            }

            foreach (var pay in req.Payments)
            {
                if (pay.Total <= 0) throw new InvalidOperationException("El pago debe ser > 0.");
                if (pay.Method is not ("cash" or "card" or "online" or "transfer"))
                    throw new InvalidOperationException("Método de pago inválido.");
            }

            var productsTotal = req.Products.Sum(x => x.Total);
            var grandTotal = req.ServiceTotal + productsTotal;
            if (Math.Round(req.Payments.Sum(p => p.Total), 2) != Math.Round(grandTotal, 2))
                throw new InvalidOperationException("La suma de los pagos debe coincidir con el total de la venta.");

            // ===== Transacción =====
            using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Crear venta
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                BranchId = booking.BranchId,
                Type = req.Products.Count > 0 ? "product" : "service", // informativo
                Total = grandTotal,
                PaymentMethod = req.Payments.Count == 1 ? req.Payments[0].Method : "mixed",
                CreatedAt = DateTimeOffset.UtcNow,
                UserId = booking.UserId,
                BookingId = booking.Id,
                Lines = new List<SaleLine>()
            };

            // Línea servicio (si hay total)
            if (req.ServiceTotal > 0)
            {
                sale.Lines.Add(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    ProductId = null, // es servicio
                    Quantity = 1,
                    UnitPrice = req.ServiceTotal,
                    Total = req.ServiceTotal
                });
            }

            // Validar stock y añadir líneas de producto
            var productIds = req.Products.Select(p => p.ProductId).ToHashSet();
            var products = await _db.Products
                .Include(p => p.Stock)
                .Where(p => productIds.Contains(p.Id) && p.BranchId == booking.BranchId && p.DeletedAt == null)
                .ToDictionaryAsync(p => p.Id, ct);

            foreach (var line in req.Products)
            {
                if (!products.TryGetValue(line.ProductId, out var prod))
                    throw new InvalidOperationException("Producto no encontrado en esta sucursal.");

                var stock = prod.Stock ?? new Stock { Id = Guid.NewGuid(), ProductId = prod.Id, MinStock = 0, CurrentStock = 0 };
                if (prod.Stock is null) _db.Stocks.Add(stock);

                // Comprobar que no quede negativo
                var after = stock.CurrentStock - line.Quantity;
                if (after < 0)
                    throw new InvalidOperationException($"Stock insuficiente para {prod.Name}.");

                sale.Lines.Add(new SaleLine
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    ProductId = prod.Id,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Total = line.Total
                });
            }

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync(ct);

            // Movimientos de stock (por cada producto) referenciados a la venta
            foreach (var line in req.Products)
            {
                var mv = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = line.ProductId,
                    Quantity = -line.Quantity, // sale = saca stock
                    Type = "sale",
                    TotalPrice = line.Total,
                    ReferenceType = "sale",
                    ReferenceId = sale.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.StockMovements.Add(mv);

                var stock = await _db.Stocks.FirstAsync(s => s.ProductId == line.ProductId, ct);
                stock.CurrentStock -= line.Quantity; // ya validado que no queda negativo
            }

            await _db.SaveChangesAsync(ct);

            // Movimiento(s) de caja por pagos en efectivo
            var cashAmount = req.Payments.Where(p => p.Method == "cash").Sum(p => p.Total);
            if (cashAmount > 0)
            {
                // encontrar sesión abierta de la sucursal
                var openSession = await _db.CashSessions
                    .FirstOrDefaultAsync(s => s.BranchId == booking.BranchId && s.ClosedAt == null, ct);
                if (openSession is null)
                    throw new InvalidOperationException("No hay sesión de caja abierta para registrar el pago en efectivo.");

                var cashMv = new CashMovement
                {
                    Id = Guid.NewGuid(),
                    SessionId = openSession.Id,
                    Date = DateTimeOffset.UtcNow,
                    Type = "income",
                    Reason = "Venta",
                    Total = cashAmount,
                    SaleId = sale.Id,
                    Note = req.Note
                };
                _db.CashMovements.Add(cashMv);
                await _db.SaveChangesAsync(ct);
            }

            // Marcar booking como completado
            booking.Status = "completed";
            booking.PaymentMethod = sale.PaymentMethod;
            booking.SaleId = sale.Id;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }
}
