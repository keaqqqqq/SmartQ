using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Authentication.Core.Entities;

namespace FNBReservation.Modules.Authentication.Core.Interfaces
{
    public interface IStaffRepository
    {
        Task<User> GetStaffByIdAsync(Guid id);
        Task<IEnumerable<User>> GetStaffByOutletIdAsync(Guid outletId);
        Task<IEnumerable<User>> GetAllStaffAsync();
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> CreateStaffAsync(User user);
        Task<User> UpdateStaffAsync(User user);
        Task<bool> DeleteStaffAsync(Guid id);
    }
}