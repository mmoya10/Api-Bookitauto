using System;
using System.Collections.Generic;
using System.Text.Json;
using NpgsqlTypes;

namespace WebApi.Domain.Entities
{
    // ===== Multitenancy =====
    public class Business
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public Guid? CategoryId { get; set; }
        public BusinessCategory? Category { get; set; }
        public string Language { get; set; } = "es";
        public string? LogoUrl { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string Status { get; set; } = "active";
        public string? Slug { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }

    public class BusinessCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Business> Businesses { get; set; } = new List<Business>();
    }

    public class Branch
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string Timezone { get; set; } = "Europe/Madrid";
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string Status { get; set; } = "active";
        public string? Slug { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<BranchSchedule> Schedules { get; set; } = new List<BranchSchedule>();
        public ICollection<BranchException> Exceptions { get; set; } = new List<BranchException>();
        public ICollection<Staff> Staff { get; set; } = new List<Staff>();
    }

    // ===== Schedules & Exceptions =====
    public class BranchSchedule
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public short Weekday { get; set; } // 0..6
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public class BranchException
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string Type { get; set; } = "closed"; // closed | special_hours
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Description { get; set; }
    }

    // ===== Roles & Permissions =====
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class Permission
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<StaffPermission> StaffPermissions { get; set; } = new List<StaffPermission>();
    }

    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }

    public class Staff
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? PasswordHash { get; set; }
        public Guid? RoleId { get; set; }
        public Role? Role { get; set; }
        public bool AvailableForBooking { get; set; } = true;
        public bool IsManager { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string Status { get; set; } = "active";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<StaffPermission> StaffPermissions { get; set; } = new List<StaffPermission>();
        public ICollection<StaffSchedule> Schedules { get; set; } = new List<StaffSchedule>();
        public ICollection<StaffException> Exceptions { get; set; } = new List<StaffException>();
    }
    public class PasswordReset
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public string TokenHash { get; set; } = null!;      // SHA256 del token
        public DateTimeOffset ExpiresAt { get; set; }       // p.ej., ahora + 30 min
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UsedAt { get; set; }         // cuando se use
    }

    public class StaffPermission
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
        public bool Active { get; set; }
    }

    public class StaffSchedule
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public short Weekday { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public class StaffException
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string Type { get; set; } = "vacation"; // vacation | sick_leave | permission | special_hours
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string Status { get; set; } = "approved"; // pending, approved, rejected
    }

    // ===== Users (clients) =====
    public class User
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }

    public class UserBusiness
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }
        public DateTimeOffset? LastBookingAt { get; set; }
        public DateTimeOffset? StartDate { get; set; }
    }

    // ===== Coupons =====
    public class Coupon
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = "percent"; // percent | fixed
        public decimal Value { get; set; }
        public string AppliesTo { get; set; } = "all";
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool Active { get; set; } = true;
        public int MaxUsesPerUser { get; set; } = 1;
        public int? MaxTotalUses { get; set; }
    }

    public class CouponUser
    {
        public Guid CouponId { get; set; }
        public Coupon Coupon { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public int Uses { get; set; }
        public int Used { get; set; }
    }

    public class CouponUsage
    {
        public Guid Id { get; set; }
        public Guid CouponId { get; set; }
        public Coupon Coupon { get; set; } = null!;
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTimeOffset UsedAt { get; set; }
        public decimal Discount { get; set; }
    }

    // ===== Products & Stock =====
    public class ProductCategory
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool Active { get; set; } = true;
    }

    public class Product
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public Guid? CategoryId { get; set; }
        public ProductCategory? Category { get; set; }
        public string Sku { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OfferPrice { get; set; }
        public DateTimeOffset? OfferStart { get; set; }
        public DateTimeOffset? OfferEnd { get; set; }
        public bool Active { get; set; } = true;
        public string? ImageUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public Stock? Stock { get; set; }
    }

    public class Stock
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int MinStock { get; set; }
        public int CurrentStock { get; set; }
    }

    public class StockMovement
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; } // +/- 
        public string Type { get; set; } = "purchase"; // purchase|adjustment|sale
        public decimal? TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? ReferenceType { get; set; } // sale|expense
        public Guid? ReferenceId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    // ===== Sales & Cash =====
    public class Expense
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public decimal? TotalPrice { get; set; }
        public int? Quantity { get; set; }
        public string? Concept { get; set; }
        public Guid? StockMovementId { get; set; }
        public StockMovement? StockMovement { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class Sale
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string Type { get; set; } = "service"; // service|product
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "cash"; // cash|card|online|mixed|transfer
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();
    }

    public class SaleLine
    {
        public Guid Id { get; set; }
        public Guid SaleId { get; set; }
        public Sale Sale { get; set; } = null!;
        public Guid? ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }

    public class CashSession
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public DateTimeOffset OpenedAt { get; set; }
        public Guid? OpenedBy { get; set; } // StaffId
        public decimal? ExpectedOpen { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public Guid? ClosedBy { get; set; }
        public decimal? ExpectedClose { get; set; }
        public string? OpeningNote { get; set; }
        public string? ClosingNote { get; set; }

        public ICollection<CashMovement> Movements { get; set; } = new List<CashMovement>();
    }

    public class CashMovement
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public CashSession Session { get; set; } = null!;
        public DateTimeOffset Date { get; set; }
        public string Type { get; set; } = "income";
        public string? Reason { get; set; }
        public decimal Total { get; set; }
        public Guid? SaleId { get; set; }
        public Sale? Sale { get; set; }
        public Guid? ExpenseId { get; set; }
        public Expense? Expense { get; set; }
        public string? Note { get; set; }
    }

    // ===== Services & Bookings =====
    public class ServiceCategory
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool Active { get; set; } = true;
    }

    public class Service
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public Guid? CategoryId { get; set; }
        public ServiceCategory? Category { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int DurationMin { get; set; }
        public int BufferBefore { get; set; }
        public int BufferAfter { get; set; }
        public bool RequiresResource { get; set; }
        public bool Active { get; set; } = true;
        public string? ImageUrl { get; set; }

        public ICollection<ServiceOption> Options { get; set; } = new List<ServiceOption>();
        public ICollection<ServiceExtra> ServiceExtras { get; set; } = new List<ServiceExtra>();
        public ICollection<ServiceResource> ServiceResources { get; set; } = new List<ServiceResource>();
    }

    public class ServiceOption
    {
        public Guid Id { get; set; }
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal PriceDelta { get; set; }
        public int DurationDelta { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class Extra
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? DurationMin { get; set; }
        public bool Active { get; set; } = true;
        public string? ImageUrl { get; set; }
    }

    public class ServiceExtra
    {
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public Guid ExtraId { get; set; }
        public Extra Extra { get; set; } = null!;
    }

    public class Resource
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int TotalQuantity { get; set; } = 1;
    }

    public class ServiceResource
    {
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public Guid ResourceId { get; set; }
        public Resource Resource { get; set; } = null!;
        public int RequiredQuantity { get; set; } = 1;
    }

    public class Booking
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid? StaffId { get; set; }
        public Staff? Staff { get; set; }
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public Guid? ServiceOptionId { get; set; }
        public ServiceOption? ServiceOption { get; set; }
        public Guid? CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        public string Status { get; set; } = "pending";
        public string? CancellationNote { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public bool OnlinePayment { get; set; }
        public string? PaymentMethod { get; set; }
        public Guid? SaleId { get; set; }
        public Sale? Sale { get; set; }

        public ICollection<BookingExtra> Extras { get; set; } = new List<BookingExtra>();
        public ICollection<BookingResource> Resources { get; set; } = new List<BookingResource>();
        public ICollection<BookingFormResponse> FormResponses { get; set; } = new List<BookingFormResponse>();
    }

    public class BookingExtra
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public Guid ExtraId { get; set; }
        public Extra Extra { get; set; } = null!;
        public decimal? Price { get; set; }
        public int? DurationMin { get; set; }
    }

    public class BookingResource
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public Guid ResourceId { get; set; }
        public Resource Resource { get; set; } = null!;
        public int ReservedQuantity { get; set; } = 1;
    }

    public class HoldSlot
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public Guid? FrontendSessionId { get; set; }
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public Guid? StaffId { get; set; }
        public Staff? Staff { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string Status { get; set; } = "active";
    }

    // ===== Booking Sites =====
    public class BookingSite
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Slug { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public bool Visible { get; set; } = true;
        public string Status { get; set; } = "draft"; // draft|published|archived
        public string DefaultFlowOrder { get; set; } = "service"; // service|staff|date
        public bool AllowAutobook { get; set; }
        public bool AutobookRequiresOnlinePayment { get; set; }
        public int AutobookMaxHoursBefore { get; set; } = 24;
        public int MinAdvanceMinutes { get; set; } = 30;
        public int MaxAdvanceDays { get; set; } = 14;
        public bool FormRequired { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public ICollection<BookingSiteService> SiteServices { get; set; } = new List<BookingSiteService>();
        public ICollection<BookingSiteStep> Steps { get; set; } = new List<BookingSiteStep>();
        public ICollection<BookingForm> Forms { get; set; } = new List<BookingForm>();
    }

    public class BookingSiteService
    {
        public Guid SiteId { get; set; }
        public BookingSite Site { get; set; } = null!;
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public bool Active { get; set; } = true;
        public int Position { get; set; }
    }

    public class BookingSiteStep
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public BookingSite Site { get; set; } = null!;
        public string Step { get; set; } = "service"; // service|staff|date|extras|form
        public int Position { get; set; }
    }

    // ===== Custom Form =====
    public class BookingForm
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public BookingSite Site { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Version { get; set; } = 1;
        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public ICollection<BookingFormField> Fields { get; set; } = new List<BookingFormField>();
    }

    public class BookingFormField
    {
        public Guid Id { get; set; }
        public Guid FormId { get; set; }
        public BookingForm Form { get; set; } = null!;
        public string Type { get; set; } = "text"; // text|textarea|number|select|checkbox|date|email|phone
        public string FieldName { get; set; } = null!;
        public string Label { get; set; } = null!;
        public bool Required { get; set; }
        public string? HelpText { get; set; }
        public string? ValidationRegex { get; set; }
        public JsonDocument? Options { get; set; } // jsonb
        public int Position { get; set; }
    }

    public class BookingFormResponse
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public Guid FormId { get; set; }
        public BookingForm Form { get; set; } = null!;
        public int FormVersion { get; set; }
        public Guid FieldId { get; set; }
        public BookingFormField Field { get; set; } = null!;
        public string FieldName { get; set; } = null!;
        public string? Value { get; set; }
        public JsonDocument? ValueJson { get; set; }
    }

    // ===== Waitlist =====
    public class WaitlistEntry
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public Guid? SiteId { get; set; }
        public BookingSite? Site { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public Guid? ServiceOptionId { get; set; }
        public ServiceOption? ServiceOption { get; set; }
        public Guid? StaffId { get; set; }
        public Staff? Staff { get; set; }
        public NpgsqlRange<DateTimeOffset> TimeWindow { get; set; }
        public string? Comments { get; set; }
        public int Priority { get; set; }
        public bool AutoBook { get; set; }
        public string Status { get; set; } = "active";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        public ICollection<WaitlistEntryExtra> Extras { get; set; } = new List<WaitlistEntryExtra>();
    }

    public class WaitlistEntryExtra
    {
        public Guid EntryId { get; set; }
        public WaitlistEntry Entry { get; set; } = null!;
        public Guid ExtraId { get; set; }
        public Extra Extra { get; set; } = null!;
    }

    public class WaitlistNotification
    {
        public Guid Id { get; set; }
        public Guid EntryId { get; set; }
        public WaitlistEntry Entry { get; set; } = null!;
        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }
        public string Channel { get; set; } = "email";
        public string? Message { get; set; }
        public string? Result { get; set; } // sent|error
        public JsonDocument? Details { get; set; } // jsonb
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class WaitlistMatchLog
    {
        public Guid Id { get; set; }
        public Guid EntryId { get; set; }
        public WaitlistEntry Entry { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public JsonDocument? Context { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    // ===== Notifications & Absences =====
    public class NotificationLog
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = null!;
        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }
        public string Name { get; set; } = null!;
        public string Channel { get; set; } = "email";
        public string Status { get; set; } = "pending";
        public int Retries { get; set; }
        public JsonDocument? PayloadJson { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class VacationCounter
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public int Year { get; set; }
        public int Total { get; set; }
        public int Used { get; set; }
    }

    public class HoursCounter
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public int Year { get; set; }
        public int Total { get; set; }
        public int Used { get; set; }
    }

    public class Absence
    {
        public Guid Id { get; set; }
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public string Type { get; set; } = "vacation"; // vacation|hours|absence
        public decimal? Hours { get; set; }
        public string Status { get; set; } = "approved";
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string? Notes { get; set; }
    }

    // jobs
    // Entity
    public class SmtpConfig
    {
        public Guid Id { get; set; }
        public Guid? BranchId { get; set; }            // null => config global plataforma
        public string Host { get; set; } = null!;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = null!;
        public string PasswordEnc { get; set; } = null!; // cifrado
        public string FromEmail { get; set; } = null!;
        public string? FromName { get; set; }
        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
    public class PaymentProviderConfig
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }         // o BusinessId si lo prefieres
        public string Provider { get; set; } = "stripe";
        public string PublicKey { get; set; } = null!;
        public string SecretKeyEnc { get; set; } = null!;
        public string? WebhookSecretEnc { get; set; }
        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
    // UNIQUE(branch_id, provider) para evitar duplicados
    public class UserDevice
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Platform { get; set; } = "fcm"; // fcm|apns
        public string DeviceToken { get; set; } = null!;
        public bool NotificationsEnabled { get; set; } = true;
        public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    }
    // UNIQUE(DeviceToken)
    // ===== Payments/Webhooks =====
    public class PaymentEvent
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = "stripe";  // stripe|...
        public string EventId { get; set; } = null!;
        public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ProcessedAt { get; set; }
        public string? Status { get; set; }               // processed|error
        public string? Error { get; set; }
    }

}
