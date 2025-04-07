// FNBReservation.Modules.Authentication.Infrastructure/Adapters/OutletAdapter.cs
using System;
using System.Threading.Tasks;
using FNBReservation.Modules.Authentication.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FNBReservation.Modules.Authentication.Infrastructure.Adapters
{
    public class OutletAdapter : IOutletAdapter
    {
        private readonly IOutletService _outletService;
        private readonly ILogger<OutletAdapter> _logger;

        public OutletAdapter(IOutletService outletService, ILogger<OutletAdapter> logger)
        {
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> OutletExistsAsync(Guid outletId)
        {
            try
            {
                var outlet = await _outletService.GetOutletByIdAsync(outletId);
                return outlet != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if outlet exists: {OutletId}", outletId);
                return false;
            }
        }

        public async Task<OutletBasicInfo> GetOutletBasicInfoAsync(Guid outletId)
        {
            try
            {
                var outlet = await _outletService.GetOutletByIdAsync(outletId);

                if (outlet == null)
                {
                    return null;
                }

                return new OutletBasicInfo
                {
                    Id = outlet.Id,
                    Name = outlet.Name,
                    IsActive = outlet.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outlet info: {OutletId}", outletId);
                return null;
            }
        }
    }
}