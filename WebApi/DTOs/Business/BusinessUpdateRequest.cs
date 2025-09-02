// BusinessUpdateRequest.cs
namespace WebApi.DTOs.Business
{
    public sealed class BusinessUpdateRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? LegalName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Website { get; init; }
        public Guid? CategoryId { get; init; }
        public string? Language { get; init; }
        public string? LogoUrl { get; init; }
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public string? Status { get; init; }            // active|inactive|suspended
        public string? Slug { get; init; }
    }
}
