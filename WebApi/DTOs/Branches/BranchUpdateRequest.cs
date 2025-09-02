// BranchUpdateRequest.cs
namespace WebApi.DTOs.Branches
{
    public sealed class BranchUpdateRequest
    {
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? Province { get; init; }
        public string? PostalCode { get; init; }
        public string? Timezone { get; init; }                 // Ej. "Europe/Madrid"
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public string? Status { get; init; }                   // active|inactive
        public string? Slug { get; init; }                     // opcional si lo gestionas
    }
}
