// BusinessDto.cs
namespace WebApi.DTOs.Business
{
    public sealed class BusinessDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public string? LegalName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Website { get; init; }
        public Guid? CategoryId { get; init; }
        public string Language { get; init; } = "es";
        public string? LogoUrl { get; init; }
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public string Status { get; init; } = "active";   // active|inactive|suspended
        public string? Slug { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
