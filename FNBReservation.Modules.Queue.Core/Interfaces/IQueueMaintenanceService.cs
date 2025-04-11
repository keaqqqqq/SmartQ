using System;
using System.Threading.Tasks;

namespace FNBReservation.Modules.Queue.Core.Interfaces
{
    public interface IQueueMaintenanceService
    {
        Task CleanupActiveQueueEntriesAsync();
    }
}