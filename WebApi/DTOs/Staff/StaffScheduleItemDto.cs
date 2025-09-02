// StaffScheduleItemDto.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffScheduleItemDto
    {
        public short Weekday { get; init; } // 0..6
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }
}
