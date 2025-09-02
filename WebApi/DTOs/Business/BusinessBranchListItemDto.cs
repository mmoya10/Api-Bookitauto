// BusinessBranchListItemDto.cs
namespace WebApi.DTOs.Business
{
    public sealed class BusinessBranchListItemDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? City { get; init; }
        public string? Province { get; init; }
        public string? Status { get; init; }
        public string? Slug { get; init; }
        public string Timezone { get; init; } = "Europe/Madrid";
        public DateTimeOffset CreatedAt { get; init; }
    }
}
