// StaffScheduleChangeRequest.cs (solicitudes puntuales por fecha)
namespace WebApi.DTOs.Staff
{
    public sealed class StaffScheduleChangeItem
    {
        public DateOnly Date { get; init; }       // día concreto
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
        public string? Note { get; init; }
    }

    public sealed class StaffScheduleChangeRequest
    {
        public List<StaffScheduleChangeItem> Items { get; init; } = new();
    }
}
