using Microsoft.EntityFrameworkCore;
using WebApi.Domain.Entities;
using WebApi.DTOs.Staff;
using WebApi.Infrastructure.Auth;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IStaffProfileService
    {
        Task<StaffMeDto> GetMeAsync(CancellationToken ct);
        Task UpdateMeAsync(StaffMeUpdateRequest req, CancellationToken ct);
        Task<StaffScheduleResponse> GetMyScheduleAsync(CancellationToken ct);
        Task<Guid> RequestScheduleChangeAsync(StaffScheduleChangeRequest req, CancellationToken ct);
        Task<object> GetCountersAsync(CancellationToken ct); // { vacations, hours }
        Task<IReadOnlyList<object>> MyAbsencesAsync(CancellationToken ct);
        Task<Guid> CreateAbsenceAsync(AbsenceCreateRequest req, CancellationToken ct);
    }

    public sealed class StaffProfileService : IStaffProfileService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUser _me;

        public StaffProfileService(AppDbContext db, ICurrentUser me)
        {
            _db = db; _me = me;
        }

        public async Task<StaffMeDto> GetMeAsync(CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");
            var s = await _db.Staff.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct)
                ?? throw new KeyNotFoundException("Staff no encontrado.");

            return new StaffMeDto
            {
                Id = s.Id,
                BranchId = s.BranchId,
                Username = s.Username,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                Phone = s.Phone,
                PhotoUrl = s.PhotoUrl,
                AvailableForBooking = s.AvailableForBooking,
                IsManager = s.IsManager,
                Status = s.Status,
                Role = s.Role?.Name ?? "staff"
            };
        }

        public async Task UpdateMeAsync(StaffMeUpdateRequest req, CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");
            var s = await _db.Staff.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct)
                ?? throw new KeyNotFoundException("Staff no encontrado.");

            if (req.FirstName is not null) s.FirstName = req.FirstName;
            if (req.LastName is not null) s.LastName = req.LastName;
            if (req.Phone is not null) s.Phone = req.Phone;
            if (req.PhotoUrl is not null) s.PhotoUrl = req.PhotoUrl;

            // Cambio de contraseña (si ambos campos presentes)
            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(s.PasswordHash))
                    throw new InvalidOperationException("Debes indicar tu contraseña actual.");

                if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, s.PasswordHash))
                    throw new InvalidOperationException("La contraseña actual no es correcta.");

                s.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            }

            s.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<StaffScheduleResponse> GetMyScheduleAsync(CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");

            var baseSched = await _db.StaffSchedules
                .Where(x => x.StaffId == id)
                .OrderBy(x => x.Weekday)
                .Select(x => new StaffScheduleItemDto
                {
                    Weekday = x.Weekday,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                })
                .ToListAsync(ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var exceptions = await _db.StaffExceptions
                .Where(e => e.StaffId == id && e.Date >= today)
                .OrderBy(e => e.Date)
                .Select(e => new
                {
                    date = e.Date,
                    type = e.Type,           // vacation|sick_leave|permission|special_hours
                    startTime = e.StartTime,
                    endTime = e.EndTime,
                    status = e.Status        // pending|approved|rejected
                })
                .ToListAsync(ct);

            return new StaffScheduleResponse { Base = baseSched, Exceptions = exceptions.Cast<object>().ToList() };
        }

        public async Task<Guid> RequestScheduleChangeAsync(StaffScheduleChangeRequest req, CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");

            if (req.Items.Count == 0)
                throw new InvalidOperationException("Debes indicar al menos un día.");

            foreach (var item in req.Items)
            {
                if (item.EndTime <= item.StartTime)
                    throw new InvalidOperationException("Cada cambio debe cumplir EndTime > StartTime.");
            }

            var now = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var toInsert = req.Items
                .Where(i => i.Date >= now)
                .Select(i => new StaffException
                {
                    Id = Guid.NewGuid(),
                    StaffId = id,
                    Date = i.Date,
                    Type = "special_hours",
                    StartTime = i.StartTime,
                    EndTime = i.EndTime,
                    Status = "pending"
                })
                .ToList();

            if (toInsert.Count == 0)
                throw new InvalidOperationException("Las fechas deben ser hoy o futuras.");

            _db.StaffExceptions.AddRange(toInsert);
            await _db.SaveChangesAsync(ct);

            // retornamos un “requestId” sintético (podrías crear una entidad dedicada si lo prefieres)
            return toInsert.First().Id;
        }

        public async Task<object> GetCountersAsync(CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");
            var year = DateTime.UtcNow.Year;

            var vac = await _db.VacationCounters.FirstOrDefaultAsync(c => c.StaffId == id && c.Year == year, ct);
            var hrs = await _db.HoursCounters.FirstOrDefaultAsync(c => c.StaffId == id && c.Year == year, ct);

            return new
            {
                vacations = new { year, total = vac?.Total ?? 0, used = vac?.Used ?? 0 },
                hours = new { year, total = hrs?.Total ?? 0, used = hrs?.Used ?? 0 }
            };
        }

        public async Task<IReadOnlyList<object>> MyAbsencesAsync(CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");

            var items = await _db.Absences
                .AsNoTracking()
                .Where(a => a.StaffId == id)
                .OrderByDescending(a => a.StartDate)
                .Select(a => new
                {
                    id = a.Id,
                    type = a.Type,     // vacation|hours|absence
                    hours = a.Hours,
                    status = a.Status, // pending|approved|rejected
                    startDate = a.StartDate,
                    endDate = a.EndDate,
                    notes = a.Notes
                })
                .ToListAsync(ct);

            return items.Cast<object>().ToList();
        }

        public async Task<Guid> CreateAbsenceAsync(AbsenceCreateRequest req, CancellationToken ct)
        {
            var id = _me.StaffId ?? throw new UnauthorizedAccessException("No autenticado.");

            if (req.EndDate <= req.StartDate)
                throw new InvalidOperationException("EndDate debe ser mayor que StartDate.");

            if (req.Type is not ("vacation" or "hours" or "absence"))
                throw new InvalidOperationException("Tipo de ausencia inválido.");

            var entity = new Absence
            {
                Id = Guid.NewGuid(),
                StaffId = id,
                Type = req.Type,
                Hours = req.Hours,
                Status = "pending",
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Notes = req.Notes
            };

            _db.Absences.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
