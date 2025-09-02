namespace WebApi.DTOs.Cash
{
    public sealed class CashSessionDto
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; init; }
        public DateTimeOffset OpenedAt { get; init; }
        public Guid? OpenedBy { get; init; }
        public DateTimeOffset? ClosedAt { get; init; }
        public Guid? ClosedBy { get; init; }
        public decimal? ExpectedOpen { get; init; }
        public decimal? ExpectedClose { get; init; }
        public string? OpeningNote { get; init; }
        public string? ClosingNote { get; init; }

        public decimal Incomes { get; set; }       // <- set
        public decimal Expenses { get; set; }      // <- set
        public decimal Adjustments { get; set; }   // <- set
        public decimal Balance { get; set; }       // <- set
    }
}
