// DTOs/Platform/Subscriptions/SubscriptionDtos.cs
namespace WebApi.DTOs.Platform.Subscriptions
{
    public sealed class PlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public decimal PricePerStaff { get; set; }
        public decimal PricePerBranch { get; set; }
        public bool Active { get; set; }
    }

    public class PlanCreateRequest
    {
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public decimal PricePerStaff { get; set; }
        public decimal PricePerBranch { get; set; }
        public bool Active { get; set; } = true;
    }

    public sealed class PlanUpdateRequest : PlanCreateRequest { }

    public sealed class BusinessSubscriptionDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public string Status { get; set; } = "active";
        public int BillingAnchorDay { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CancelAt { get; set; }
    }

    public sealed class AssignSubscriptionRequest
    {
        public Guid PlanId { get; set; }
        public int? BillingAnchorDay { get; set; } // si null, hoy
    }

    public sealed class PlatformInvoiceDto
    {
        public Guid Id { get; set; }         // SubscriptionUsage.Id
        public Guid BusinessId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "pending"; // pending|charged|failed
        public string? ProviderInvoiceId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
