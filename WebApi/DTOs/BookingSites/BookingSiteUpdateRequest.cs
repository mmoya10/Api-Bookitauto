// BookingSiteUpdateRequest.cs
namespace WebApi.DTOs.BookingSites
{
    public sealed class BookingSiteUpdateRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Slug { get; init; }
        public bool? IsPrimary { get; init; }
        public bool? Visible { get; init; }
        public string? Status { get; init; } // draft|published|archived
        public string? DefaultFlowOrder { get; init; } // service|staff|date
        public bool? AllowAutobook { get; init; }
        public bool? AutobookRequiresOnlinePayment { get; init; }
        public int? AutobookMaxHoursBefore { get; init; }
        public int? MinAdvanceMinutes { get; init; }
        public int? MaxAdvanceDays { get; init; }
        public bool? FormRequired { get; init; }
    }
}
