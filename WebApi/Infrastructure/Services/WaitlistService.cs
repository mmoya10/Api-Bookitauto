using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Waitlist;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Persistence;
using WebApi.Domain.Entities;

namespace WebApi.Infrastructure.Services
{
    public interface IWaitlistService
    {
        Task<Guid> CreateAsync(WaitlistCreateRequest req, CancellationToken ct);
        Task<IReadOnlyList<WaitlistEntryDto>> MineAsync(CancellationToken ct);
        Task DeleteAsync(Guid entryId, CancellationToken ct);
    }

    public sealed class WaitlistService : IWaitlistService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUser _me;

        public WaitlistService(AppDbContext db, ICurrentUser me)
        {
            _db = db; _me = me;
        }

        // ➕ Crear entrada en la lista de espera
        public async Task<Guid> CreateAsync(WaitlistCreateRequest req, CancellationToken ct)
        {
            if (_me.UserId is null)
                throw new InvalidOperationException("Solo usuarios autenticados pueden crear entradas.");

            if (req.To <= req.From)
                throw new InvalidOperationException("El rango horario es inválido.");

            var entry = new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                ServiceId = req.ServiceId,
                ServiceOptionId = req.ServiceOptionId,
                StaffId = req.StaffId,
                UserId = _me.UserId,
                TimeWindow = new NpgsqlTypes.NpgsqlRange<DateTimeOffset>(req.From, req.To),
                Comments = req.Comments,
                AutoBook = req.AutoBook,
                Status = "active",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.WaitlistEntries.Add(entry);
            await _db.SaveChangesAsync(ct);
            return entry.Id;
        }

        // 👤 Listar mis entradas de waitlist
        public async Task<IReadOnlyList<WaitlistEntryDto>> MineAsync(CancellationToken ct)
        {
            if (_me.UserId is null)
                throw new InvalidOperationException("Usuario no autenticado.");

            return await _db.WaitlistEntries.AsNoTracking()
                .Where(w => w.UserId == _me.UserId && w.Status == "active")
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WaitlistEntryDto
                {
                    Id = w.Id,
                    BranchId = w.BranchId,
                    ServiceId = w.ServiceId,
                    ServiceOptionId = w.ServiceOptionId,
                    StaffId = w.StaffId,
                    From = w.TimeWindow.LowerBound,
                    To = w.TimeWindow.UpperBound,
                    Comments = w.Comments,
                    AutoBook = w.AutoBook,
                    Status = w.Status,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync(ct);
        }

        // ❌ Eliminar (cancelar) una entrada de waitlist
        public async Task DeleteAsync(Guid entryId, CancellationToken ct)
        {
            if (_me.UserId is null)
                throw new InvalidOperationException("Usuario no autenticado.");

            var entry = await _db.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == entryId && w.UserId == _me.UserId, ct)
                ?? throw new KeyNotFoundException("Entrada no encontrada.");

            entry.Status = "cancelled";
            await _db.SaveChangesAsync(ct);
        }
    }
}
