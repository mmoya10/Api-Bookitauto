// WaitlistCreateRequest.cs
namespace WebApi.DTOs.Waitlist
{
    public sealed class WaitlistCreateRequest
    {
        public Guid BranchId { get; init; }
        public Guid ServiceId { get; init; }
        public Guid? ServiceOptionId { get; init; }
        public Guid? StaffId { get; init; }
        public DateTimeOffset From { get; init; }     // inicio de ventana
        public DateTimeOffset To { get; init; }       // fin de ventana
        public string? Comments { get; init; }
        public bool AutoBook { get; init; } = false;
    }
}
