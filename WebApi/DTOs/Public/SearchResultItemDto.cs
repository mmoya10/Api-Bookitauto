// SearchResultItemDto.cs
namespace WebApi.DTOs.Public
{
    public sealed class SearchResultItemDto
    {
        public Guid BusinessId { get; init; }
        public Guid? BranchId { get; init; }
        public string BusinessName { get; init; } = null!;
        public string? BranchName { get; init; }
        public string? City { get; init; }
        public string? Province { get; init; }
        public string? LogoUrl { get; init; }
        public string? Category { get; init; }
        public string? Slug { get; init; }          // slug del negocio o de la branch si la usas públicamente
        public int ServicesCount { get; init; }
    }
}
