using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;
using ReservationTableInfo = FNBReservation.Modules.Reservation.Core.Interfaces.TableInfo;

namespace FNBReservation.Modules.Reservation.Infrastructure.Adapters
{
    public class OutletAdapter : IOutletAdapter
    {
        private readonly IOutletService _outletService;
        private readonly ITableService _tableService;
        private readonly IPeakHourService _peakHourService;
        private readonly ILogger<OutletAdapter> _logger;

        public OutletAdapter(
            IOutletService outletService,
            ITableService tableService,
            IPeakHourService peakHourService,
            ILogger<OutletAdapter> logger)
        {
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
            _peakHourService = peakHourService ?? throw new ArgumentNullException(nameof(peakHourService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OutletInfo> GetOutletInfoAsync(Guid outletId)
        {
            _logger.LogInformation("Getting outlet info for outlet: {OutletId}", outletId);

            try
            {
                var outlet = await _outletService.GetOutletByIdAsync(outletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", outletId);
                    return null;
                }

                return new OutletInfo
                {
                    Id = outlet.Id,
                    Name = outlet.Name,
                    DefaultDiningDurationMinutes = outlet.DefaultDiningDurationMinutes,
                    MaxAdvanceReservationTime = outlet.MaxAdvanceReservationTime,
                    MinAdvanceReservationTime = outlet.MinAdvanceReservationTime,
                    IsActive = outlet.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outlet info: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<IEnumerable<ReservationTableInfo>> GetTablesAsync(Guid outletId)
        {
            _logger.LogInformation("Getting tables for outlet: {OutletId}", outletId);

            try
            {
                var tables = await _tableService.GetTablesByOutletIdAsync(outletId);
                if (tables == null || !tables.Any())
                {
                    _logger.LogWarning("No tables found for outlet: {OutletId}", outletId);
                    return Enumerable.Empty<ReservationTableInfo>();
                }

                return tables.Where(t => t.IsActive).Select(t => new ReservationTableInfo
                {
                    Id = t.Id,
                    TableNumber = t.TableNumber,
                    Capacity = t.Capacity,
                    Section = t.Section,
                    IsActive = t.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<ReservationSettings> GetReservationSettingsAsync(Guid outletId, DateTime dateTime)
        {
            _logger.LogInformation("Getting reservation settings for outlet: {OutletId} at {DateTime}", outletId, dateTime);

            try
            {
                // Get outlet basic info
                var outlet = await _outletService.GetOutletByIdAsync(outletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", outletId);
                    return null;
                }

                // Get current reservation allocation percent (applies peak hour settings if applicable)
                int reservationAllocationPercent = await _peakHourService.GetCurrentReservationAllocationAsync(outletId, dateTime);

                if (reservationAllocationPercent <= 0 && outlet.ReservationAllocationPercent > 0)
                {
                    _logger.LogWarning("Peak hour service returned zero allocation. Using outlet default: {DefaultAllocation}%",
                        outlet.ReservationAllocationPercent);
                    reservationAllocationPercent = outlet.ReservationAllocationPercent;
                }

                // Get total tables capacity
                int totalCapacity = await _tableService.GetTotalTablesCapacityAsync(outletId);

                // Calculate reservation capacity based on allocation percentage
                int reservationCapacity = (int)Math.Ceiling(totalCapacity * (reservationAllocationPercent / 100.0));

                return new ReservationSettings
                {
                    ReservationAllocationPercent = reservationAllocationPercent,
                    DefaultDiningDurationMinutes = outlet.DefaultDiningDurationMinutes,
                    TotalCapacity = totalCapacity,
                    ReservationCapacity = reservationCapacity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation settings: {OutletId}", outletId);
                throw;
            }
        }
    }
}