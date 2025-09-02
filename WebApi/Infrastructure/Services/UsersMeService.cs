using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Users;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IUsersMeService
    {
        Task<UserMeDto> GetAsync(CancellationToken ct);
        Task UpdateAsync(UserMeUpdateRequest req, CancellationToken ct);
        Task<IReadOnlyList<FrequentBranchItemDto>> GetFrequentBranchesAsync(int take, CancellationToken ct);
        Task<IReadOnlyList<MyBookingListItemDto>> GetMyBookingsAsync(string? status, CancellationToken ct);
    }

    public sealed class UsersMeService : IUsersMeService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUser _me;

        public UsersMeService(AppDbContext db, ICurrentUser me)
        {
            _db = db; _me = me;
        }

        public async Task<UserMeDto> GetAsync(CancellationToken ct)
        {
            var uid = _me.UserId ?? throw new InvalidOperationException("Token inválido: usuario no identificado.");
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == uid, ct)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            return new UserMeDto
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                PhotoUrl = u.PhotoUrl,
                Active = u.Active,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            };
        }

        public async Task UpdateAsync(UserMeUpdateRequest req, CancellationToken ct)
        {
            var uid = _me.UserId ?? throw new InvalidOperationException("Token inválido: usuario no identificado.");
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == uid, ct)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            if (req.Email is not null)
            {
                var email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
                if (email is not null)
                {
                    var dup = await _db.Users.AnyAsync(x => x.Email == email && x.Id != uid, ct);
                    if (dup) throw new InvalidOperationException("Ese email ya está en uso.");
                }
                u.Email = email;
            }

            if (req.Username is not null)  u.Username  = string.IsNullOrWhiteSpace(req.Username) ? null : req.Username.Trim();
            if (req.FirstName is not null) u.FirstName = req.FirstName;
            if (req.LastName is not null)  u.LastName  = req.LastName;
            if (req.Phone is not null)     u.Phone     = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            if (req.PhotoUrl is not null)  u.PhotoUrl  = req.PhotoUrl;

            u.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<FrequentBranchItemDto>> GetFrequentBranchesAsync(int take, CancellationToken ct)
        {
            var uid = _me.UserId ?? throw new InvalidOperationException("Token inválido: usuario no identificado.");

            return await _db.UserBusinesses.AsNoTracking()
                .Where(ub => ub.UserId == uid)
                .OrderByDescending(ub => ub.LastBookingAt ?? DateTimeOffset.MinValue)
                .Take(take)
                .Select(ub => new FrequentBranchItemDto
                {
                    BranchId = ub.BranchId ?? Guid.Empty,
                    BusinessId = ub.BusinessId,
                    BranchName = ub.BranchId != null ? ub.Branch!.Name : "(sin sucursal)",
                    City = ub.BranchId != null ? ub.Branch!.City : null,
                    Province = ub.BranchId != null ? ub.Branch!.Province : null,
                    LastBookingAt = ub.LastBookingAt
                })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<MyBookingListItemDto>> GetMyBookingsAsync(string? status, CancellationToken ct)
        {
            var uid = _me.UserId ?? throw new InvalidOperationException("Token inválido: usuario no identificado.");
            var now = DateTimeOffset.UtcNow;

            var q = _db.Bookings.AsNoTracking()
                .Where(b => b.UserId == uid);

            // Filtro por estado de vista (no confundir con b.Status exacto)
            // - upcoming: futuras y no canceladas
            // - past: ya pasadas
            // - (otro): filtra por estado exacto
            if (string.Equals(status, "upcoming", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.StartTime >= now && b.Status != "cancelled");
            else if (string.Equals(status, "past", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.StartTime < now);
            else if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(b => b.Status == status);

            return await q
                .OrderByDescending(b => b.StartTime)
                .Select(b => new MyBookingListItemDto
                {
                    Id = b.Id,
                    BranchId = b.BranchId,
                    BranchName = b.Branch.Name,
                    ServiceId = b.ServiceId,
                    ServiceName = b.Service.Name,
                    StaffId = b.StaffId,
                    StaffName = b.StaffId != null ? (b.Staff!.FirstName + " " + b.Staff!.LastName) : null,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    OnlinePayment = b.OnlinePayment
                })
                .ToListAsync(ct);
        }
    }
}
