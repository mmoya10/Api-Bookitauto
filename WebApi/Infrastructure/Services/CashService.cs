using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Cash;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface ICashService
    {
        Task<(CashSessionDto? open, IReadOnlyList<CashSessionDto> last)> GetSessionsAsync(Guid branchId, int take, CancellationToken ct);
        Task<Guid> OpenAsync(Guid branchId, CashSessionOpenRequest req, CancellationToken ct);
        Task CloseAsync(Guid sessionId, CashSessionCloseRequest req, CancellationToken ct);

        Task<(IReadOnlyList<CashMovementDto> items, int total)> GetMovementsAsync(Guid sessionId, int page, int size, CancellationToken ct);
        Task<Guid> CreateMovementAsync(Guid sessionId, CashMovementCreateRequest req, CancellationToken ct);
        Task DeleteMovementAsync(Guid movementId, CancellationToken ct);
    }

    public sealed class CashService : ICashService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUser _me;

        public CashService(AppDbContext db, ICurrentUser me)
        {
            _db = db; _me = me;
        }

        // ===== Sesiones =====

        public async Task<(CashSessionDto? open, IReadOnlyList<CashSessionDto> last)> GetSessionsAsync(Guid branchId, int take, CancellationToken ct)
        {
            var open = await _db.CashSessions
                .AsNoTracking()
                .Where(s => s.BranchId == branchId && s.ClosedAt == null)
                .Select(s => MapSession(s))
                .FirstOrDefaultAsync(ct);

            if (open is not null)
                open = await WithTotalsAsync(open, ct);

            var last = await _db.CashSessions
                .AsNoTracking()
                .Where(s => s.BranchId == branchId)
                .OrderByDescending(s => s.OpenedAt)
                .Take(take)
                .Select(s => MapSession(s))
                .ToListAsync(ct);

            // Totales por cada una (rápido para `take` pequeño)
            var result = new List<CashSessionDto>(last.Count);
            foreach (var s in last)
                result.Add(await WithTotalsAsync(s, ct));

            return (open, result);
        }

        public async Task<Guid> OpenAsync(Guid branchId, CashSessionOpenRequest req, CancellationToken ct)
        {
            var existing = await _db.CashSessions.AnyAsync(s => s.BranchId == branchId && s.ClosedAt == null, ct);
            if (existing) throw new InvalidOperationException("Ya existe una sesión de caja abierta para esta sucursal.");

            var session = new Domain.Entities.CashSession
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                OpenedAt = DateTimeOffset.UtcNow,
                OpenedBy = _me.StaffId,
                ExpectedOpen = req.ExpectedOpen,
                OpeningNote = req.OpeningNote
            };

            _db.CashSessions.Add(session);
            await _db.SaveChangesAsync(ct);
            return session.Id;
        }

        public async Task CloseAsync(Guid sessionId, CashSessionCloseRequest req, CancellationToken ct)
        {
            var s = await _db.CashSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
                ?? throw new KeyNotFoundException("Sesión no encontrada.");

            if (s.ClosedAt is not null)
                throw new InvalidOperationException("La sesión ya está cerrada.");

            s.ClosedAt = DateTimeOffset.UtcNow;
            s.ClosedBy = _me.StaffId;
            s.ExpectedClose = req.ExpectedClose;
            s.ClosingNote = req.ClosingNote;

            await _db.SaveChangesAsync(ct);
        }

        // ===== Movimientos =====

        public async Task<(IReadOnlyList<CashMovementDto> items, int total)> GetMovementsAsync(Guid sessionId, int page, int size, CancellationToken ct)
        {
            var q = _db.CashMovements
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId);

            var total = await q.CountAsync(ct);

            var items = await q.OrderByDescending(m => m.Date)
                .Skip((page - 1) * size).Take(size)
                .Select(m => new CashMovementDto
                {
                    Id = m.Id,
                    SessionId = m.SessionId,
                    Date = m.Date,
                    Type = m.Type,
                    Reason = m.Reason,
                    Total = m.Total,
                    SaleId = m.SaleId,
                    ExpenseId = m.ExpenseId,
                    Note = m.Note
                })
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<Guid> CreateMovementAsync(Guid sessionId, CashMovementCreateRequest req, CancellationToken ct)
        {
            if (req.Total <= 0) throw new InvalidOperationException("El importe debe ser mayor que 0.");
            if (req.Type is not ("income" or "expense" or "adjustment"))
                throw new InvalidOperationException("Tipo inválido (income|expense|adjustment).");

            var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
                ?? throw new KeyNotFoundException("Sesión no encontrada.");

            if (session.ClosedAt is not null)
                throw new InvalidOperationException("La sesión está cerrada.");

            // Referencias coherentes
            if (req.SaleId.HasValue && req.ExpenseId.HasValue)
                throw new InvalidOperationException("No puede referenciar venta y gasto a la vez.");

            var movement = new Domain.Entities.CashMovement
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Date = DateTimeOffset.UtcNow,
                Type = req.Type,
                Reason = req.Reason,
                Total = req.Total,
                SaleId = req.SaleId,
                ExpenseId = req.ExpenseId,
                Note = req.Note
            };

            _db.CashMovements.Add(movement);
            await _db.SaveChangesAsync(ct);
            return movement.Id;
        }

        public async Task DeleteMovementAsync(Guid movementId, CancellationToken ct)
        {
            var mv = await _db.CashMovements
                .Include(m => m.Session)
                .FirstOrDefaultAsync(m => m.Id == movementId, ct)
                ?? throw new KeyNotFoundException("Movimiento no encontrado.");

            if (mv.Session.ClosedAt is not null)
                throw new InvalidOperationException("La sesión está cerrada.");

            if (mv.SaleId.HasValue || mv.ExpenseId.HasValue)
                throw new InvalidOperationException("No se puede eliminar un movimiento vinculado a una venta o gasto.");

            _db.CashMovements.Remove(mv);
            await _db.SaveChangesAsync(ct);
        }

        // ===== Helpers =====

        private static CashSessionDto MapSession(Domain.Entities.CashSession s) => new()
        {
            Id = s.Id,
            BranchId = s.BranchId,
            OpenedAt = s.OpenedAt,
            OpenedBy = s.OpenedBy,
            ClosedAt = s.ClosedAt,
            ClosedBy = s.ClosedBy,
            ExpectedOpen = s.ExpectedOpen,
            ExpectedClose = s.ExpectedClose,
            OpeningNote = s.OpeningNote,
            ClosingNote = s.ClosingNote
        };

        private async Task<CashSessionDto> WithTotalsAsync(CashSessionDto dto, CancellationToken ct)
        {
            var sums = await _db.CashMovements
                .Where(m => m.SessionId == dto.Id)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    incomes = g.Where(x => x.Type == "income").Sum(x => (decimal?)x.Total) ?? 0m,
                    expenses = g.Where(x => x.Type == "expense").Sum(x => (decimal?)x.Total) ?? 0m,
                    adjustments = g.Where(x => x.Type == "adjustment").Sum(x => (decimal?)x.Total) ?? 0m
                })
                .FirstOrDefaultAsync(ct) ?? new { incomes = 0m, expenses = 0m, adjustments = 0m };

            dto.Incomes = sums.incomes;
            dto.Expenses = sums.expenses;
            dto.Adjustments = sums.adjustments;
            dto.Balance = sums.incomes - sums.expenses + sums.adjustments;
            return dto;
        }

    }
}
