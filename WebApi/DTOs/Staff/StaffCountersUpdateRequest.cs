// StaffCountersUpdateRequest.cs
namespace WebApi.DTOs.Staff
{
    public sealed class StaffCountersUpdateRequest
    {
        public int? VacationTotal { get; init; }
        public int? HoursTotal { get; init; }
    }
}
