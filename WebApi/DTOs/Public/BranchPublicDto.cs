// BranchPublicDto.cs
namespace WebApi.DTOs.Public
{
    public sealed class BranchPublicDto
    {
        public Guid Id { get; init; }
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? Province { get; init; }
        public string? PostalCode { get; init; }
        public string Timezone { get; init; } = "Europe/Madrid";
        public string? Slug { get; init; }

        public IReadOnlyList<ServiceItem> Services { get; init; } = Array.Empty<ServiceItem>();
        public IReadOnlyList<ScheduleItem> Schedule { get; init; } = Array.Empty<ScheduleItem>();

        public sealed class ServiceItem
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = null!;
            public string? Description { get; init; }
            public decimal BasePrice { get; init; }
            public int DurationMin { get; init; }
            public string? ImageUrl { get; init; }

            public IReadOnlyList<OptionItem> Options { get; init; } = Array.Empty<OptionItem>();
            public IReadOnlyList<ExtraItem> Extras { get; init; } = Array.Empty<ExtraItem>();
        }

        public sealed class OptionItem
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = null!;
            public decimal PriceDelta { get; init; }
            public int DurationDelta { get; init; }
            public string? ImageUrl { get; init; }
        }

        public sealed class ExtraItem
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = null!;
            public decimal? Price { get; init; }
            public int? DurationMin { get; init; }
            public string? ImageUrl { get; init; }
        }

        public sealed class ScheduleItem
        {
            public short Weekday { get; init; } // 0..6
            public TimeOnly StartTime { get; init; }
            public TimeOnly EndTime { get; init; }
        }
    }
}
