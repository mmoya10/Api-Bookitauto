// AbsenceCreateRequest.cs (para el propio staff)
namespace WebApi.DTOs.Staff
{
    public sealed class AbsenceCreateRequest
    {
        public string Type { get; init; } = "vacation"; // vacation|hours|absence
        public decimal? Hours { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
        public string? Notes { get; init; }
    }
}
