// BranchScheduleDto.cs
namespace WebApi.DTOs.Branches
{
    public sealed class BranchScheduleDto
    {
        public Guid Id { get; init; }
        public short Weekday { get; init; }                    // 0..6 (domingo..sábado o como lo uses)
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }
}
