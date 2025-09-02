// StaffScheduleResponse.cs (respuesta de /schedule)
namespace WebApi.DTOs.Staff
{
    public sealed class StaffScheduleResponse
    {
        public List<StaffScheduleItemDto> Base { get; init; } = new();
        public List<object> Exceptions { get; init; } = new(); // { date, type, startTime?, endTime?, status }
    }
}
