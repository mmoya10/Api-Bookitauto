// ServiceDto.cs  (si ya lo tienes, déjalo)
namespace WebApi.DTOs.Services
{
    public sealed class ServiceDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public Guid? CategoryId { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public decimal BasePrice { get; init; }
        public int DurationMin { get; init; }
        public int BufferBefore { get; init; }
        public int BufferAfter { get; init; }
        public bool RequiresResource { get; init; }
        public bool Active { get; init; }
        public string? ImageUrl { get; init; }

        public IReadOnlyList<ServiceOptionItem> Options { get; init; } = Array.Empty<ServiceOptionItem>();
        public IReadOnlyList<ServiceExtraItem> Extras { get; init; } = Array.Empty<ServiceExtraItem>();

        public sealed class ServiceOptionItem
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = null!;
            public decimal PriceDelta { get; init; }
            public int DurationDelta { get; init; }
            public string? ImageUrl { get; init; }
        }

        public sealed class ServiceExtraItem
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = null!;
            public decimal? Price { get; init; }
            public int? DurationMin { get; init; }
            public bool Active { get; init; }
        }
    }
}
