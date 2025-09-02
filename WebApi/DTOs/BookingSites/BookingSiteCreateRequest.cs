// BookingSiteCreateRequest.cs
namespace WebApi.DTOs.BookingSites
{
    public sealed class BookingSiteCreateRequest
    {
        public string Name { get; init; } = null!;
        public string Slug { get; init; } = null!;
        public string? Description { get; init; }
        public bool IsPrimary { get; init; }
        public bool Visible { get; init; } = true;
        public string Status { get; init; } = "draft";
        public string DefaultFlowOrder { get; init; } = "service";
        public bool AllowAutobook { get; init; }
        public bool AutobookRequiresOnlinePayment { get; init; }
        public int AutobookMaxHoursBefore { get; init; } = 24;
        public int MinAdvanceMinutes { get; init; } = 30;
        public int MaxAdvanceDays { get; init; } = 14;
        public bool FormRequired { get; init; } = true;
    }
}
