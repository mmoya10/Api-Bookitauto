// DTOs/Platform/Accounts/AccountListItemDto.cs
namespace WebApi.DTOs.Platform.Accounts
{
    public sealed class AccountListItemDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = null!;
        public string? LegalName { get; set; }
        public string Status { get; set; } = null!;
        public int Branches { get; set; }
        public int Staff { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AccountCreateRequest
    {
        public string Name { get; set; } = null!;
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Slug { get; set; }
    }

    public sealed class AccountUpdateRequest : AccountCreateRequest
    {
        public string? Status { get; set; } // active|inactive|suspended
    }

    public sealed class StaffListItemDto
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = "Staff";
        public bool IsManager { get; set; }
        public string Status { get; set; } = "active";
    }

    public sealed class ImpersonateResponse
    {
        public string Token { get; set; } = null!;
        public Guid StaffId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? BranchId { get; set; }
    }
}
