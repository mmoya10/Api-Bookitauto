// SubscriptionDto.cs
namespace WebApi.DTOs.Billing
{
    public sealed class SubscriptionDto
    {
        public string Plan { get; init; } = "starter";
        public string Status { get; init; } = "active";   // active|past_due|canceled|trialing
        public DateTimeOffset? CurrentPeriodEnd { get; init; }
        public bool CancelAtPeriodEnd { get; init; }
        public string? Provider { get; init; }            // p.ej. Stripe
        public string? ExternalId { get; init; }          // id de la suscripción en el PSP
    }
}
