// FNBReservation.Modules.Authentication.Core/Interfaces/IOutletAdapter.cs
using System;
using System.Threading.Tasks;

namespace FNBReservation.Modules.Authentication.Core.Interfaces
{
    public interface IOutletAdapter
    {
        Task<bool> OutletExistsAsync(Guid outletId);
        Task<OutletBasicInfo> GetOutletBasicInfoAsync(Guid outletId);
    }

    public class OutletBasicInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}