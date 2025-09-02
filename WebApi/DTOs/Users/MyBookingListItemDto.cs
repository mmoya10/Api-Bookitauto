// MyBookingListItemDto.cs
namespace WebApi.DTOs.Users
{
    public sealed class MyBookingListItemDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public string BranchName { get; init; } = null!;
        public string ServiceName { get; init; } = null!;
        public Guid ServiceId { get; init; }
        public Guid? StaffId { get; init; }
        public string? StaffName { get; init; }
        public DateTimeOffset StartTime { get; init; }
        public DateTimeOffset EndTime { get; init; }
        public string Status { get; init; } = null!;
        public decimal TotalPrice { get; init; }
        public bool OnlinePayment { get; init; }
    }
}
