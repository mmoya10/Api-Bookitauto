// CouponUpdateRequest.cs
namespace WebApi.DTOs.Coupons
{
    public sealed class CouponUpdateRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Type { get; init; }                 // percent|fixed
        public decimal? Value { get; init; }
        public string? AppliesTo { get; init; }            // all|specific_user|first_booking
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public bool? Active { get; init; }
        public int? MaxUsesPerUser { get; init; }
        public int? MaxTotalUses { get; init; }
    }
}
