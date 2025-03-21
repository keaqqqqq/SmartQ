// FNBReservation.Modules.Authentication.Core/Interfaces/IStaffService.cs (new file)
using FNBReservation.Modules.Authentication.Core.DTOs;

namespace FNBReservation.Modules.Authentication.Core.Interfaces
{
    public interface IStaffService
    {
        Task<StaffDto> CreateStaffAsync(CreateStaffDto createStaffDto, Guid adminId);
        Task<StaffDto> GetStaffByIdAsync(Guid id);
        Task<IEnumerable<StaffDto>> GetStaffByOutletIdAsync(Guid outletId);
        Task<IEnumerable<StaffDto>> GetAllStaffAsync();
        Task<StaffDto> UpdateStaffAsync(Guid id, UpdateStaffDto updateStaffDto, Guid adminId);
        Task<bool> DeleteStaffAsync(Guid id);
    }
}