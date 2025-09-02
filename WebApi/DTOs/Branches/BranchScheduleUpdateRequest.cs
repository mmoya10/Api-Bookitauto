// BranchScheduleUpdateRequest.cs
namespace WebApi.DTOs.Branches
{
    public sealed class BranchScheduleUpdateRequest
    {
        public IReadOnlyList<BranchScheduleItem> Items { get; init; } = Array.Empty<BranchScheduleItem>();

        public sealed class BranchScheduleItem
        {
            public short Weekday { get; init; }                // 0..6
            public TimeOnly StartTime { get; init; }
            public TimeOnly EndTime { get; init; }
        }
    }
}
