// BookingSiteDto.cs
namespace WebApi.DTOs.BookingSites
{
    public sealed class BookingSiteDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public string Slug { get; init; } = null!;
        public bool IsPrimary { get; init; }
        public bool Visible { get; init; }
        public string Status { get; init; } = "draft"; // draft|published|archived
        public string DefaultFlowOrder { get; init; } = "service";
        public bool AllowAutobook { get; init; }
        public bool AutobookRequiresOnlinePayment { get; init; }
        public int AutobookMaxHoursBefore { get; init; }
        public int MinAdvanceMinutes { get; init; }
        public int MaxAdvanceDays { get; init; }
        public bool FormRequired { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
