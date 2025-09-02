// CouponCreateRequest.cs
namespace WebApi.DTOs.Coupons
{
    public sealed class CouponCreateRequest
    {
        public string Code { get; init; } = null!;
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string Type { get; init; } = "percent";           // percent|fixed
        public decimal Value { get; init; }
        public string AppliesTo { get; init; } = "all";           // all|specific_user|first_booking
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public bool Active { get; init; } = true;
        public int MaxUsesPerUser { get; init; } = 1;
        public int? MaxTotalUses { get; init; }
    }
}
