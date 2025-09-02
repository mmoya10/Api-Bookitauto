using Microsoft.EntityFrameworkCore;
using WebApi.DTOs.Absences;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Infrastructure.Services
{
    public interface IAbsenceService
    {
        Task UpdateAsync(Guid absenceId, Guid branchId, AbsenceUpdateRequest req, CancellationToken ct);
        Task DeleteAsync(Guid absenceId, Guid branchId, CancellationToken ct);
    }

    public sealed class AbsenceService : IAbsenceService
    {
        private readonly AppDbContext _db;
        public AbsenceService(AppDbContext db) => _db = db;

        public async Task UpdateAsync(Guid absenceId, Guid branchId, AbsenceUpdateRequest req, CancellationToken ct)
        {
            var a = await _db.Absences
                .Include(x => x.Staff)
                .FirstOrDefaultAsync(x => x.Id == absenceId, ct)
                ?? throw new KeyNotFoundException("Ausencia no encontrada.");

            if (a.Staff.BranchId != branchId)
                throw new InvalidOperationException("La ausencia no pertenece a esta sucursal.");

            // Guardamos estado previo para ajustar contadores si procede
            var wasApproved = a.Status == "approved";
            var prevType = a.Type;
            var prevHours = a.Hours ?? 0m;
            var prevDays = CountVacationDays(a.StartDate, a.EndDate);

            // Validaciones y asignaciones
            if (req.Type is not null)
            {
                if (req.Type is not ("vacation" or "hours" or "absence"))
                    throw new InvalidOperationException("Tipo inválido (vacation|hours|absence).");
                a.Type = req.Type;
            }

            if (req.StartDate.HasValue) a.StartDate = req.StartDate.Value;
            if (req.EndDate.HasValue)   a.EndDate   = req.EndDate.Value;

            if (a.EndDate < a.StartDate)
                throw new InvalidOperationException("EndDate no puede ser anterior a StartDate.");

            if (req.Hours.HasValue)
            {
                if (req.Hours.Value < 0) throw new InvalidOperationException("Hours debe ser >= 0.");
                a.Hours = req.Hours.Value;
            }

            if (req.Notes is not null) a.Notes = req.Notes;

            if (req.Status is not null)
            {
                if (req.Status is not ("pending" or "approved" or "rejected"))
                    throw new InvalidOperationException("Status inválido (pending|approved|rejected).");
                a.Status = req.Status;
            }

            // Ajuste de contadores (modelo simple):
            var isApproved = a.Status == "approved";

            // Si antes estaba aprobado y ahora deja de estarlo → restar lo que contaba
            if (wasApproved && !isApproved)
                await AdjustCountersAsync(a, branchId, remove: true, prevType, prevHours, prevDays, ct);

            // Si ahora está aprobado:
            //  - si antes no lo estaba → sumar todo
            //  - si ya lo estaba pero cambió duración/tipo/horas → ajustar diferencia
            if (isApproved)
            {
                if (!wasApproved)
                {
                    var addDays = CountVacationDays(a.StartDate, a.EndDate);
                    await AdjustCountersAsync(a, branchId, remove: false, a.Type, a.Hours ?? 0m, addDays, ct);
                }
                else
                {
                    // Estaba aprobado y puede haber cambiado rango/horas/tipo
                    var newDays = CountVacationDays(a.StartDate, a.EndDate);
                    // primero deshacer lo previo...
                    await AdjustCountersAsync(a, branchId, remove: true, prevType, prevHours, prevDays, ct);
                    // ...y luego aplicar lo nuevo
                    await AdjustCountersAsync(a, branchId, remove: false, a.Type, a.Hours ?? 0m, newDays, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid absenceId, Guid branchId, CancellationToken ct)
        {
            var a = await _db.Absences
                .Include(x => x.Staff)
                .FirstOrDefaultAsync(x => x.Id == absenceId, ct)
                ?? throw new KeyNotFoundException("Ausencia no encontrada.");

            if (a.Staff.BranchId != branchId)
                throw new InvalidOperationException("La ausencia no pertenece a esta sucursal.");

            // Si estaba aprobada, revertir contadores
            if (a.Status == "approved")
            {
                var prevDays = CountVacationDays(a.StartDate, a.EndDate);
                await AdjustCountersAsync(a, branchId, remove: true, a.Type, a.Hours ?? 0m, prevDays, ct);
            }

            _db.Absences.Remove(a);
            await _db.SaveChangesAsync(ct);
        }

        // === Helpers ===

        private static int CountVacationDays(DateTimeOffset start, DateTimeOffset end)
        {
            // días naturales inclusive (simple)
            var s = start.Date;
            var e = end.Date;
            var days = (e - s).TotalDays + 1;
            return days < 0 ? 0 : (int)days;
        }

        private async Task AdjustCountersAsync(
            Domain.Entities.Absence a,
            Guid branchId,
            bool remove,
            string type,
            decimal hours,
            int days,
            CancellationToken ct)
        {
            var sign = remove ? -1 : 1;

            if (type == "vacation" && days > 0)
            {
                var year = a.StartDate.Year;
                var vc = await _db.VacationCounters
                    .FirstOrDefaultAsync(x => x.StaffId == a.StaffId && x.Year == year, ct);

                if (vc is null)
                {
                    // Si quitamos y no existe, nada que hacer; si sumamos, crear contador
                    if (!remove)
                    {
                        vc = new Domain.Entities.VacationCounter
                        {
                            Id = Guid.NewGuid(),
                            StaffId = a.StaffId,
                            Year = year,
                            Total = 0,
                            Used = 0
                        };
                        _db.VacationCounters.Add(vc);
                    }
                }

                if (vc is not null)
                {
                    vc.Used += sign * days;
                    if (vc.Used < 0) vc.Used = 0; // clamp básico
                }
            }
            else if (type == "hours" && hours > 0)
            {
                var year = a.StartDate.Year;
                var hc = await _db.HoursCounters
                    .FirstOrDefaultAsync(x => x.StaffId == a.StaffId && x.Year == year, ct);

                if (hc is null)
                {
                    if (!remove)
                    {
                        hc = new Domain.Entities.HoursCounter
                        {
                            Id = Guid.NewGuid(),
                            StaffId = a.StaffId,
                            Year = year,
                            Total = 0,
                            Used = 0
                        };
                        _db.HoursCounters.Add(hc);
                    }
                }

                if (hc is not null)
                {
                    // HoursCounter.Used es int; convertimos redondeando hacia arriba
                    var delta = (int)Math.Ceiling(hours);
                    hc.Used += sign * delta;
                    if (hc.Used < 0) hc.Used = 0;
                }
            }
            // type == "absence": no ajusta contadores
        }
    }
}
