// DTOs/Platform/Tickets/TicketDtos.cs
namespace WebApi.DTOs.Platform.Tickets
{
    public sealed class TicketDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? BranchId { get; set; }
        public string Subject { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Severity { get; set; } = "normal";
        public string Status { get; set; } = "open";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
    }

    public sealed class TicketCreateRequest
    {
        public Guid BusinessId { get; set; }
        public Guid? BranchId { get; set; }
        public string Subject { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Severity { get; set; } = "normal"; // low|normal|high|urgent
    }

    public sealed class TicketUpdateRequest
    {
        public string? Status { get; set; } // open|in_progress|resolved|closed
        public string? ResolutionNote { get; set; }
    }
}
