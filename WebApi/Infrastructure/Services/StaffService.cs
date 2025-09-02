using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.DTOs.Staff;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IStaffService
    {
        Task<IReadOnlyList<StaffListItemDto>> ListAsync(Guid branchId, CancellationToken ct);
        Task<Guid> CreateAsync(Guid branchId, StaffCreateRequest req, CancellationToken ct);
        Task UpdateAsync(Guid id, StaffUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
        Task UpdateCountersAsync(Guid id, StaffCountersUpdateRequest req, CancellationToken ct);
        Task UpdateScheduleAsync(Guid id, IEnumerable<StaffScheduleUpdateRequest> req, CancellationToken ct);
    }

    public sealed class StaffService : IStaffService
    {
        private readonly AppDbContext _db;
        public StaffService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<StaffListItemDto>> ListAsync(Guid branchId, CancellationToken ct)
        {
            return await _db.Staff
                .AsNoTracking()
                .Include(s => s.Role)
                .Where(s => s.BranchId == branchId && s.DeletedAt == null)
                .Select(s => new StaffListItemDto
                {
                    Id = s.Id,
                    Username = s.Username,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    Phone = s.Phone,
                    Role = s.Role != null ? s.Role.Name : "staff",
                    AvailableForBooking = s.AvailableForBooking,
                    IsManager = s.IsManager,
                    Status = s.Status,
                    PhotoUrl = s.PhotoUrl
                })
                .ToListAsync(ct);
        }

        public async Task<Guid> CreateAsync(Guid branchId, StaffCreateRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                throw new InvalidOperationException("Email requerido para staff.");

            var exists = await _db.Staff.AnyAsync(
                s => s.BranchId == branchId && s.Email == req.Email && s.DeletedAt == null, ct);
            if (exists)
                throw new InvalidOperationException("Ya existe un staff con ese email en esta sucursal.");

            var entity = new Staff
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Username = req.Username,
                FirstName = req.FirstName,
                LastName = req.LastName,
                Email = req.Email,
                Phone = req.Phone,
                PasswordHash = string.IsNullOrEmpty(req.Password) ? null : BCrypt.Net.BCrypt.HashPassword(req.Password),
                RoleId = req.RoleId,
                AvailableForBooking = req.AvailableForBooking,
                IsManager = req.IsManager,
                PhotoUrl = req.PhotoUrl,
                Status = "active",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Staff.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Guid id, StaffUpdateRequest req, CancellationToken ct)
        {
            var s = await _db.Staff.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
            if (s is null) throw new KeyNotFoundException("Staff no encontrado.");

            if (!string.IsNullOrEmpty(req.Email) && req.Email != s.Email)
            {
                var exists = await _db.Staff.AnyAsync(x => x.BranchId == s.BranchId && x.Email == req.Email && x.Id != id, ct);
                if (exists) throw new InvalidOperationException("Email ya en uso en esta sucursal.");
                s.Email = req.Email;
            }

            if (!string.IsNullOrWhiteSpace(req.Password))
                s.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            if (req.Username is not null) s.Username = req.Username;
            if (req.FirstName is not null) s.FirstName = req.FirstName;
            if (req.LastName is not null) s.LastName = req.LastName;
            if (req.Phone is not null) s.Phone = req.Phone;
            if (req.RoleId.HasValue) s.RoleId = req.RoleId;
            if (req.AvailableForBooking.HasValue) s.AvailableForBooking = req.AvailableForBooking.Value;
            if (req.IsManager.HasValue) s.IsManager = req.IsManager.Value;
            if (req.PhotoUrl is not null) s.PhotoUrl = req.PhotoUrl;
            if (req.Status is not null) s.Status = req.Status;

            s.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var s = await _db.Staff.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
            if (s is null) return;
            s.Status = "inactive";
            s.DeletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateCountersAsync(Guid id, StaffCountersUpdateRequest req, CancellationToken ct)
        {
            var staff = await _db.Staff.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null, ct);
            if (staff is null) throw new KeyNotFoundException("Staff no encontrado.");

            if (req.VacationTotal.HasValue)
            {
                var counter = await _db.VacationCounters.FirstOrDefaultAsync(c => c.StaffId == id && c.Year == DateTime.UtcNow.Year, ct);
                if (counter is null)
                {
                    counter = new VacationCounter { Id = Guid.NewGuid(), StaffId = id, Year = DateTime.UtcNow.Year, Total = req.VacationTotal.Value, Used = 0 };
                    _db.VacationCounters.Add(counter);
                }
                else
                {
                    counter.Total = req.VacationTotal.Value;
                }
            }

            if (req.HoursTotal.HasValue)
            {
                var counter = await _db.HoursCounters.FirstOrDefaultAsync(c => c.StaffId == id && c.Year == DateTime.UtcNow.Year, ct);
                if (counter is null)
                {
                    counter = new HoursCounter { Id = Guid.NewGuid(), StaffId = id, Year = DateTime.UtcNow.Year, Total = req.HoursTotal.Value, Used = 0 };
                    _db.HoursCounters.Add(counter);
                }
                else
                {
                    counter.Total = req.HoursTotal.Value;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateScheduleAsync(Guid id, IEnumerable<StaffScheduleUpdateRequest> req, CancellationToken ct)
        {
            var staff = await _db.Staff.Include(s => s.Schedules).FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null, ct);
            if (staff is null) throw new KeyNotFoundException("Staff no encontrado.");

            staff.Schedules.Clear();
            foreach (var s in req)
            {
                if (s.EndTime <= s.StartTime)
                    throw new InvalidOperationException("El horario debe tener EndTime > StartTime");

                staff.Schedules.Add(new StaffSchedule
                {
                    Id = Guid.NewGuid(),
                    StaffId = id,
                    Weekday = s.Weekday,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
