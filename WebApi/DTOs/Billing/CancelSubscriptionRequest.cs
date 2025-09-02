// CancelSubscriptionRequest.cs
namespace WebApi.DTOs.Billing
{
    public sealed class CancelSubscriptionRequest
    {
        public string? Reason { get; init; }
        public string? ContactEmail { get; init; }   // si quieres que te respondan a otro correo
    }
}
