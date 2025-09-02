// BusinessPublicDto.cs
namespace WebApi.DTOs.Public
{
    public sealed class BusinessPublicDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public string? Website { get; init; }
        public string? LogoUrl { get; init; }
        public string? Category { get; init; }
        public string? Slug { get; init; }
        public IReadOnlyList<BusinessBranchItem> Branches { get; init; } = Array.Empty<BusinessBranchItem>();

        public sealed class BusinessBranchItem
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = null!;
            public string? City { get; init; }
            public string? Province { get; init; }
            public string Timezone { get; init; } = "Europe/Madrid";
            public string Status { get; init; } = "active";
            public string? Slug { get; init; }
        }
    }
}
