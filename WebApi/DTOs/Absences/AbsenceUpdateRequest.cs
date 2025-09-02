namespace WebApi.DTOs.Absences
{
    public sealed class AbsenceUpdateRequest
    {
        // Cambios permitidos por admin/admin-branch
        public string? Type { get; init; }              // vacation | hours | absence
        public decimal? Hours { get; init; }            // solo si Type=hours
        public string? Status { get; init; }            // pending | approved | rejected
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public string? Notes { get; init; }
    }
}
