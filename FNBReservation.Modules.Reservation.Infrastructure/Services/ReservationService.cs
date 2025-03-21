using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Entities;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;
using ReservationTableInfo = FNBReservation.Modules.Reservation.Core.Interfaces.TableInfo;


namespace FNBReservation.Modules.Reservation.Infrastructure.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IReservationNotificationService _notificationService;
        private readonly IOutletAdapter _outletAdapter;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            IReservationRepository reservationRepository,
            IReservationNotificationService notificationService,
            IOutletAdapter outletAdapter,
            ILogger<ReservationService> logger)
        {
            _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _outletAdapter = outletAdapter ?? throw new ArgumentNullException(nameof(outletAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // In FNBReservation.Modules.Reservation.Infrastructure/Services/ReservationService.cs
        // Modify the CheckAvailabilityAsync method to use more flexible time slots
        // and improve preferred time matching

        public async Task<TimeSlotAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto request)
        {
            _logger.LogInformation("Checking availability for outlet {OutletId}, party size {PartySize}, date {Date}",
                request.OutletId, request.PartySize, request.Date);

            try
            {
                // Get outlet information
                var outlet = await _outletAdapter.GetOutletInfoAsync(request.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", request.OutletId);
                    throw new ArgumentException($"Outlet with ID {request.OutletId} not found");
                }

                if (!outlet.IsActive)
                {
                    _logger.LogWarning("Outlet is inactive: {OutletId}", request.OutletId);
                    throw new ArgumentException($"Outlet with ID {request.OutletId} is not active");
                }

                // Parse preferred time if provided
                TimeSpan? preferredTime = request.PreferredTime;

                // Set default or use provided time range
                TimeSpan earliestTime = request.EarliestTime ?? new TimeSpan(11, 0, 0); // Default 11:00 AM
                TimeSpan latestTime = request.LatestTime ?? new TimeSpan(21, 0, 0);     // Default 9:00 PM

                // Create response object
                var response = new TimeSlotAvailabilityResponseDto
                {
                    OutletId = request.OutletId,
                    OutletName = outlet.Name,
                    PartySize = request.PartySize,
                    Date = request.Date.Date,
                    AvailableTimeSlots = new List<AvailableTimeslotDto>(),
                    AlternativeTimeSlots = new List<AvailableTimeslotDto>()
                };

                // Generate time slots at 30-minute intervals
                DateTime startDateTime = request.Date.Date.Add(earliestTime);
                DateTime endDateTime = request.Date.Date.Add(latestTime);
                TimeSpan interval = TimeSpan.FromMinutes(30); // Change to 30-minute intervals

                // Define exact preferred time if provided
                DateTime? exactPreferredDateTime = null;
                if (preferredTime.HasValue)
                {
                    exactPreferredDateTime = request.Date.Date.Add(preferredTime.Value);
                    _logger.LogInformation("Preferred time: {PreferredTime}", exactPreferredDateTime);
                }

                // Check each time slot
                for (DateTime slotTime = startDateTime; slotTime <= endDateTime; slotTime = slotTime.Add(interval))
                {
                    // For each time slot, check availability based on outlet's settings for that time
                    var settings = await _outletAdapter.GetReservationSettingsAsync(request.OutletId, slotTime);

                    if (settings == null)
                    {
                        _logger.LogWarning("Could not get reservation settings for outlet {OutletId} at {DateTime}",
                            request.OutletId, slotTime);
                        continue;
                    }

                    // If reservation allocation is 0%, skip this time slot
                    if (settings.ReservationAllocationPercent <= 0)
                    {
                        _logger.LogInformation("Skipping time slot {TimeSlot} as reservation allocation is {Allocation}%",
                            slotTime, settings.ReservationAllocationPercent);
                        continue;
                    }

                    // Calculate the end time for this reservation based on the dining duration
                    DateTime endTime = slotTime.AddMinutes(settings.DefaultDiningDurationMinutes);

                    // Get the current reserved capacity for this time slot
                    int reservedCapacity = await _reservationRepository.GetReservedCapacityForTimeSlotAsync(
                        request.OutletId, slotTime, endTime);

                    // Calculate available capacity
                    int availableCapacity = settings.ReservationCapacity - reservedCapacity;

                    // Check if the party can be accommodated
                    bool isAvailable = availableCapacity >= request.PartySize;

                    // Check if this is the preferred time slot
                    bool isPreferred = false;
                    if (exactPreferredDateTime.HasValue)
                    {
                        // Check if the current slot is within 15 minutes of the preferred time
                        var timeDiff = Math.Abs((slotTime - exactPreferredDateTime.Value).TotalMinutes);
                        isPreferred = timeDiff <= 15;

                        // If this is exactly the preferred time, ensure it's first in the list
                        if (slotTime.TimeOfDay == exactPreferredDateTime.Value.TimeOfDay)
                        {
                            isPreferred = true; // Extra sure it's marked as preferred
                        }
                    }

                    var timeSlot = new AvailableTimeslotDto
                    {
                        DateTime = slotTime,
                        AvailableCapacity = availableCapacity,
                        IsPreferred = isPreferred
                    };

                    // Add to appropriate list
                    if (isAvailable)
                    {
                        if (isPreferred)
                        {
                            // Add preferred times to the beginning of the list
                            response.AvailableTimeSlots.Insert(0, timeSlot);
                        }
                        else
                        {
                            response.AvailableTimeSlots.Add(timeSlot);
                        }
                    }
                    else if (availableCapacity > 0)
                    {
                        // Add as alternative if has some capacity (useful for smaller party suggestions)
                        response.AlternativeTimeSlots.Add(timeSlot);
                    }
                }

                // If the client requested 19:00 specifically and it's not in the slots, add it if possible
                if (exactPreferredDateTime.HasValue && !response.AvailableTimeSlots.Any(ts => ts.DateTime.TimeOfDay == exactPreferredDateTime.Value.TimeOfDay))
                {
                    // See if we can add the exact preferred time
                    var settings = await _outletAdapter.GetReservationSettingsAsync(request.OutletId, exactPreferredDateTime.Value);
                    if (settings != null && settings.ReservationAllocationPercent > 0)
                    {
                        DateTime endTime = exactPreferredDateTime.Value.AddMinutes(settings.DefaultDiningDurationMinutes);
                        int reservedCapacity = await _reservationRepository.GetReservedCapacityForTimeSlotAsync(
                            request.OutletId, exactPreferredDateTime.Value, endTime);
                        int availableCapacity = settings.ReservationCapacity - reservedCapacity;

                        var exactTimeSlot = new AvailableTimeslotDto
                        {
                            DateTime = exactPreferredDateTime.Value,
                            AvailableCapacity = availableCapacity,
                            IsPreferred = true
                        };

                        if (availableCapacity >= request.PartySize)
                        {
                            // Insert at the beginning since it's the preferred time
                            response.AvailableTimeSlots.Insert(0, exactTimeSlot);
                        }
                        else if (availableCapacity > 0)
                        {
                            // Add to alternatives if there's some capacity
                            response.AlternativeTimeSlots.Add(exactTimeSlot);
                        }
                    }
                }

                // Sort time slots by time (keeping preferred at the top)
                response.AvailableTimeSlots = response.AvailableTimeSlots
                    .OrderBy(ts => !ts.IsPreferred) // Put preferred times first
                    .ThenBy(ts => ts.DateTime)
                    .ToList();

                response.AlternativeTimeSlots = response.AlternativeTimeSlots
                    .OrderBy(ts => ts.DateTime)
                    .ToList();

                _logger.LogInformation("Found {AvailableCount} available time slots and {AlternativeCount} alternative time slots",
                    response.AvailableTimeSlots.Count, response.AlternativeTimeSlots.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for outlet {OutletId}", request.OutletId);
                throw;
            }
        }

        public async Task<ReservationDto> CreateReservationAsync(CreateReservationDto createReservationDto)
        {
            _logger.LogInformation("Creating reservation for {CustomerName} at outlet {OutletId}",
                createReservationDto.CustomerName, createReservationDto.OutletId);

            try
            {
                // Validate outlet exists and is active
                var outlet = await _outletAdapter.GetOutletInfoAsync(createReservationDto.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", createReservationDto.OutletId);
                    throw new ArgumentException($"Outlet with ID {createReservationDto.OutletId} not found");
                }

                if (!outlet.IsActive)
                {
                    _logger.LogWarning("Outlet is inactive: {OutletId}", createReservationDto.OutletId);
                    throw new ArgumentException($"Outlet with ID {createReservationDto.OutletId} is not active");
                }

                // Check reservation is within allowed time range
                DateTime now = DateTime.UtcNow;
                DateTime minAllowedTime = now.AddHours(outlet.MinAdvanceReservationTime);
                DateTime maxAllowedTime = now.AddDays(outlet.MaxAdvanceReservationTime);

                if (createReservationDto.ReservationDate < minAllowedTime)
                {
                    _logger.LogWarning("Reservation time is too soon: {ReservationDate}", createReservationDto.ReservationDate);
                    throw new ArgumentException($"Reservations must be made at least {outlet.MinAdvanceReservationTime} hours in advance");
                }

                if (createReservationDto.ReservationDate > maxAllowedTime)
                {
                    _logger.LogWarning("Reservation time is too far in advance: {ReservationDate}", createReservationDto.ReservationDate);
                    throw new ArgumentException($"Reservations can only be made up to {outlet.MaxAdvanceReservationTime} days in advance");
                }

                // Check if the time slot is available
                var settings = await _outletAdapter.GetReservationSettingsAsync(createReservationDto.OutletId, createReservationDto.ReservationDate);
                if (settings == null)
                {
                    _logger.LogWarning("Could not get reservation settings for outlet {OutletId}", createReservationDto.OutletId);
                    throw new InvalidOperationException("Could not retrieve reservation settings");
                }

                // If reservation allocation is 0%, reject the reservation
                if (settings.ReservationAllocationPercent <= 0)
                {
                    _logger.LogWarning("Reservations not allowed at this time. Allocation: {Allocation}%", settings.ReservationAllocationPercent);
                    throw new InvalidOperationException("Reservations are not accepted for this time slot");
                }

                // Calculate duration based on outlet settings
                TimeSpan duration = TimeSpan.FromMinutes(settings.DefaultDiningDurationMinutes);

                // Check if there's capacity for this reservation
                DateTime endTime = createReservationDto.ReservationDate.Add(duration);
                int reservedCapacity = await _reservationRepository.GetReservedCapacityForTimeSlotAsync(
                    createReservationDto.OutletId, createReservationDto.ReservationDate, endTime);

                int availableCapacity = settings.ReservationCapacity - reservedCapacity;

                if (availableCapacity < createReservationDto.PartySize)
                {
                    _logger.LogWarning("Not enough capacity for party size {PartySize}. Available: {AvailableCapacity}",
                        createReservationDto.PartySize, availableCapacity);
                    throw new InvalidOperationException("Not enough capacity for this reservation");
                }

                // Generate reservation code
                string reservationCode = GenerateReservationCode();

                // Create reservation entity
                var reservation = new ReservationEntity
                {
                    Id = Guid.NewGuid(),
                    ReservationCode = reservationCode,
                    OutletId = createReservationDto.OutletId,
                    CustomerName = createReservationDto.CustomerName,
                    CustomerPhone = createReservationDto.CustomerPhone,
                    CustomerEmail = createReservationDto.CustomerEmail,
                    PartySize = createReservationDto.PartySize,
                    ReservationDate = createReservationDto.ReservationDate,
                    Duration = duration,
                    Status = "Confirmed", // Start as confirmed
                    SpecialRequests = createReservationDto.SpecialRequests,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save reservation
                var createdReservation = await _reservationRepository.CreateAsync(reservation);

                // Get available tables for assignment
                var availableTables = await _outletAdapter.GetTablesAsync(createReservationDto.OutletId);
                var tablesToAssign = await FindOptimalTableCombinationAsync(
                    availableTables.ToList(),
                    createReservationDto.PartySize,
                    createReservationDto.OutletId,
                    createReservationDto.ReservationDate,
                    createReservationDto.ReservationDate.Add(duration)
                );

                if (tablesToAssign == null || tablesToAssign.Count == 0)
                {
                    _logger.LogWarning("No suitable tables found for party size {PartySize}", createReservationDto.PartySize);
                    // Proceed without table assignments for now
                }
                else
                {
                    // Assign tables
                    foreach (var table in tablesToAssign)
                    {
                        var tableAssignment = new ReservationTableAssignment
                        {
                            Id = Guid.NewGuid(),
                            ReservationId = createdReservation.Id,
                            TableId = table.Id,
                            TableNumber = table.TableNumber,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _reservationRepository.AddTableAssignmentAsync(tableAssignment);
                    }
                }

                // Add initial status change
                var statusChange = new ReservationStatusChange
                {
                    Id = Guid.NewGuid(),
                    ReservationId = createdReservation.Id,
                    OldStatus = "",
                    NewStatus = "Confirmed",
                    ChangedAt = DateTime.UtcNow,
                    Reason = "Initial reservation"
                };

                await _reservationRepository.AddStatusChangeAsync(statusChange);

                // Schedule reminders
                await ScheduleRemindersAsync(createdReservation.Id, createdReservation.ReservationDate);

                // Send confirmation notification
                await _notificationService.SendConfirmationAsync(createdReservation.Id);

                // Return DTO
                return await MapReservationToDto(createdReservation, outlet.Name, tablesToAssign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation for {CustomerName} at outlet {OutletId}",
                    createReservationDto.CustomerName, createReservationDto.OutletId);
                throw;
            }
        }

        public async Task<ReservationDto> GetReservationByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting reservation by ID: {ReservationId}", id);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", id);
                    return null;
                }

                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                    // Return with unknown outlet name
                    return await MapReservationToDto(reservation, "Unknown Outlet", new List<ReservationTableInfo>());
                }

                var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                var assignedTables = tables
                    .Where(t => reservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                    .ToList();

                return await MapReservationToDto(reservation, outlet.Name, assignedTables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation: {ReservationId}", id);
                throw;
            }
        }

        public async Task<ReservationDto> GetReservationByCodeAsync(string reservationCode)
        {
            _logger.LogInformation("Getting reservation by code: {ReservationCode}", reservationCode);

            try
            {
                var reservation = await _reservationRepository.GetByCodeAsync(reservationCode);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationCode}", reservationCode);
                    return null;
                }

                return await GetReservationByIdAsync(reservation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by code: {ReservationCode}", reservationCode);
                throw;
            }
        }

        public async Task<IEnumerable<ReservationDto>> GetReservationsByOutletIdAsync(Guid outletId, DateTime? date = null, string status = null)
        {
            _logger.LogInformation("Getting reservations for outlet: {OutletId}, Date: {Date}, Status: {Status}",
                outletId, date, status);

            try
            {
                var reservations = await _reservationRepository.GetByOutletIdAsync(outletId, date, status);

                var outlet = await _outletAdapter.GetOutletInfoAsync(outletId);
                string outletName = outlet?.Name ?? "Unknown Outlet";

                var tables = await _outletAdapter.GetTablesAsync(outletId);

                var reservationDtos = new List<ReservationDto>();
                foreach (var reservation in reservations)
                {
                    var assignedTables = tables
                        .Where(t => reservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();

                    reservationDtos.Add(await MapReservationToDto(reservation, outletName, assignedTables));
                }

                return reservationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<IEnumerable<ReservationDto>> GetReservationsByPhoneAsync(string phone)
        {
            _logger.LogInformation("Getting reservations for phone: {Phone}", phone);

            try
            {
                var reservations = await _reservationRepository.GetByPhoneAsync(phone);

                var reservationDtos = new List<ReservationDto>();
                foreach (var reservation in reservations)
                {
                    var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                    string outletName = outlet?.Name ?? "Unknown Outlet";

                    var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var assignedTables = tables
                        .Where(t => reservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();

                    reservationDtos.Add(await MapReservationToDto(reservation, outletName, assignedTables));
                }

                return reservationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for phone: {Phone}", phone);
                throw;
            }
        }

        public async Task<ReservationDto> UpdateReservationAsync(Guid id, UpdateReservationDto updateReservationDto)
        {
            _logger.LogInformation("Updating reservation: {ReservationId}", id);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", id);
                    return null;
                }

                // Don't allow updates to canceled reservations
                if (reservation.Status == "Canceled" || reservation.Status == "NoShow")
                {
                    _logger.LogWarning("Cannot update {Status} reservation: {ReservationId}", reservation.Status, id);
                    throw new InvalidOperationException($"Cannot update a reservation with status '{reservation.Status}'");
                }

                // Track changes for notification
                List<string> changes = new List<string>();

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateReservationDto.CustomerName) &&
                    updateReservationDto.CustomerName != reservation.CustomerName)
                {
                    changes.Add($"Name updated from {reservation.CustomerName} to {updateReservationDto.CustomerName}");
                    reservation.CustomerName = updateReservationDto.CustomerName;
                }

                if (!string.IsNullOrEmpty(updateReservationDto.CustomerPhone) &&
                    updateReservationDto.CustomerPhone != reservation.CustomerPhone)
                {
                    changes.Add($"Phone updated from {reservation.CustomerPhone} to {updateReservationDto.CustomerPhone}");
                    reservation.CustomerPhone = updateReservationDto.CustomerPhone;
                }

                if (!string.IsNullOrEmpty(updateReservationDto.CustomerEmail) &&
                    updateReservationDto.CustomerEmail != reservation.CustomerEmail)
                {
                    changes.Add($"Email updated from {reservation.CustomerEmail} to {updateReservationDto.CustomerEmail}");
                    reservation.CustomerEmail = updateReservationDto.CustomerEmail;
                }

                if (updateReservationDto.SpecialRequests != null &&
                    updateReservationDto.SpecialRequests != reservation.SpecialRequests)
                {
                    changes.Add("Special requests updated");
                    reservation.SpecialRequests = updateReservationDto.SpecialRequests;
                }

                // Handle more complex updates that need validation
                if (updateReservationDto.PartySize.HasValue || updateReservationDto.ReservationDate.HasValue)
                {
                    // Get outlet and settings
                    var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                    if (outlet == null)
                    {
                        _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                        throw new InvalidOperationException("Cannot update reservation: outlet not found");
                    }

                    // Handle party size change
                    int newPartySize = updateReservationDto.PartySize ?? reservation.PartySize;

                    // Handle date change
                    DateTime newReservationDate = updateReservationDto.ReservationDate ?? reservation.ReservationDate;

                    // If either changed, we need to validate capacity
                    if (newPartySize != reservation.PartySize || newReservationDate != reservation.ReservationDate)
                    {
                        // Check reservation limits if date changed
                        if (newReservationDate != reservation.ReservationDate)
                        {
                            DateTime now = DateTime.UtcNow;
                            DateTime minAllowedTime = now.AddHours(outlet.MinAdvanceReservationTime);
                            DateTime maxAllowedTime = now.AddDays(outlet.MaxAdvanceReservationTime);

                            if (newReservationDate < minAllowedTime)
                            {
                                _logger.LogWarning("New reservation time is too soon: {ReservationDate}", newReservationDate);
                                throw new ArgumentException($"Reservations must be made at least {outlet.MinAdvanceReservationTime} hours in advance");
                            }

                            if (newReservationDate > maxAllowedTime)
                            {
                                _logger.LogWarning("New reservation time is too far in advance: {ReservationDate}", newReservationDate);
                                throw new ArgumentException($"Reservations can only be made up to {outlet.MaxAdvanceReservationTime} days in advance");
                            }
                        }

                        // Get settings for new date/time
                        var settings = await _outletAdapter.GetReservationSettingsAsync(
                            reservation.OutletId, newReservationDate);

                        if (settings == null)
                        {
                            _logger.LogWarning("Could not get reservation settings for outlet {OutletId}", reservation.OutletId);
                            throw new InvalidOperationException("Could not retrieve reservation settings");
                        }

                        // If reservation allocation is 0%, reject the change
                        if (settings.ReservationAllocationPercent <= 0)
                        {
                            _logger.LogWarning("Reservations not allowed at this time. Allocation: {Allocation}%",
                                settings.ReservationAllocationPercent);
                            throw new InvalidOperationException("Reservations are not accepted for this time slot");
                        }

                        // Update duration if needed
                        TimeSpan newDuration = TimeSpan.FromMinutes(settings.DefaultDiningDurationMinutes);

                        // Check capacity for the new time/size
                        DateTime endTime = newReservationDate.Add(newDuration);
                        int reservedCapacity = await _reservationRepository.GetReservedCapacityForTimeSlotAsync(
                            reservation.OutletId, newReservationDate, endTime);

                        // Subtract current reservation's capacity from the total
                        if (newReservationDate == reservation.ReservationDate)
                        {
                            reservedCapacity -= reservation.PartySize;
                        }

                        int availableCapacity = settings.ReservationCapacity - reservedCapacity;

                        if (availableCapacity < newPartySize)
                        {
                            _logger.LogWarning("Not enough capacity for new party size {PartySize}. Available: {AvailableCapacity}",
                                newPartySize, availableCapacity);
                            throw new InvalidOperationException("Not enough capacity for the updated reservation");
                        }

                        // Update party size
                        if (newPartySize != reservation.PartySize)
                        {
                            changes.Add($"Party size updated from {reservation.PartySize} to {newPartySize}");
                            reservation.PartySize = newPartySize;
                        }

                        // Update reservation date
                        if (newReservationDate != reservation.ReservationDate)
                        {
                            changes.Add($"Date/time updated from {reservation.ReservationDate} to {newReservationDate}");
                            reservation.ReservationDate = newReservationDate;
                            reservation.Duration = newDuration;
                        }

                        // If anything changed, we need to re-assign tables
                        if (changes.Any())
                        {
                            // Clear current table assignments
                            foreach (var assignment in reservation.TableAssignments.ToList())
                            {
                                await _reservationRepository.RemoveTableAssignmentAsync(reservation.Id, assignment.TableId);
                            }

                            // Assign new tables
                            var availableTables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                            var tablesToAssign = await FindOptimalTableCombinationAsync(
                                availableTables.ToList(),
                                newPartySize,
                                reservation.OutletId,
                                newReservationDate,
                                newReservationDate.Add(newDuration)
                            );

                            if (tablesToAssign != null && tablesToAssign.Count > 0)
                            {
                                foreach (var table in tablesToAssign)
                                {
                                    var tableAssignment = new ReservationTableAssignment
                                    {
                                        Id = Guid.NewGuid(),
                                        ReservationId = reservation.Id,
                                        TableId = table.Id,
                                        TableNumber = table.TableNumber,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    await _reservationRepository.AddTableAssignmentAsync(tableAssignment);
                                }
                            }
                            else
                            {
                                // No tables available - should handle this case
                                throw new InvalidOperationException("No suitable tables available for this reservation time");
                            }

                            // If date changed, reschedule reminders
                            if (newReservationDate != reservation.ReservationDate)
                            {
                                await ScheduleRemindersAsync(reservation.Id, newReservationDate);
                            }
                        }
                    }
                }

                if (changes.Any())
                {
                    // Update reservation
                    reservation.UpdatedAt = DateTime.UtcNow;
                    var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

                    // Notify customer of changes
                    await _notificationService.SendModificationAsync(reservation.Id, string.Join(", ", changes));

                    // Get updated table assignments
                    var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                    var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var assignedTables = tables
                        .Where(t => updatedReservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();

                    // Return updated reservation
                    return await MapReservationToDto(updatedReservation, outlet?.Name ?? "Unknown Outlet", assignedTables);
                }
                else
                {
                    _logger.LogInformation("No changes detected for reservation: {ReservationId}", id);

                    // Get updated information even if no changes
                    var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                    var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var assignedTables = tables
                        .Where(t => reservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();

                    return await MapReservationToDto(reservation, outlet?.Name ?? "Unknown Outlet", assignedTables);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", id);
                throw;
            }
        }

        public async Task<ReservationDto> CancelReservationAsync(Guid id, CancelReservationDto cancelReservationDto)
        {
            _logger.LogInformation("Canceling reservation: {ReservationId}", id);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", id);
                    return null;
                }

                // Don't allow canceling already canceled reservations
                if (reservation.Status == "Canceled")
                {
                    _logger.LogWarning("Reservation already canceled: {ReservationId}", id);
                    // Return current state
                    var outletInfo = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                    var tablesInfo = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var assignedTablesInfo = tablesInfo
                        .Where(t => reservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();
                    return await MapReservationToDto(reservation, outletInfo?.Name ?? "Unknown Outlet", assignedTablesInfo);
                }

                // Don't allow canceling completed reservations
                if (reservation.Status == "Completed")
                {
                    _logger.LogWarning("Cannot cancel completed reservation: {ReservationId}", id);
                    throw new InvalidOperationException("Cannot cancel a completed reservation");
                }

                // Record status change
                var oldStatus = reservation.Status;
                reservation.Status = "Canceled";
                reservation.UpdatedAt = DateTime.UtcNow;

                // Add status change record
                var statusChange = new ReservationStatusChange
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    OldStatus = oldStatus,
                    NewStatus = "Canceled",
                    ChangedAt = DateTime.UtcNow,
                    Reason = cancelReservationDto.Reason
                };

                await _reservationRepository.AddStatusChangeAsync(statusChange);

                // Update reservation
                var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

                // Notify customer
                await _notificationService.SendCancellationAsync(reservation.Id, cancelReservationDto.Reason);

                // Return updated reservation
                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                var assignedTables = tables
                    .Where(t => updatedReservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                    .ToList();

                return await MapReservationToDto(updatedReservation, outlet?.Name ?? "Unknown Outlet", assignedTables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling reservation: {ReservationId}", id);
                throw;
            }
        }

        public async Task<bool> ConfirmReservationAsync(Guid id)
        {
            _logger.LogInformation("Confirming reservation: {ReservationId}", id);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", id);
                    return false;
                }

                // Only allow confirming pending reservations
                if (reservation.Status != "Pending")
                {
                    _logger.LogWarning("Cannot confirm {Status} reservation: {ReservationId}", reservation.Status, id);
                    return false;
                }

                // Record status change
                var oldStatus = reservation.Status;
                reservation.Status = "Confirmed";
                reservation.UpdatedAt = DateTime.UtcNow;

                // Add status change record
                var statusChange = new ReservationStatusChange
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    OldStatus = oldStatus,
                    NewStatus = "Confirmed",
                    ChangedAt = DateTime.UtcNow,
                    Reason = "Reservation confirmed"
                };

                await _reservationRepository.AddStatusChangeAsync(statusChange);

                // Update reservation
                await _reservationRepository.UpdateAsync(reservation);

                // Send confirmation
                await _notificationService.SendConfirmationAsync(reservation.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming reservation: {ReservationId}", id);
                return false;
            }
        }

        public async Task<bool> MarkAsNoShowAsync(Guid id)
        {
            _logger.LogInformation("Marking reservation as no-show: {ReservationId}", id);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", id);
                    return false;
                }

                // Only allow marking confirmed reservations as no-show
                if (reservation.Status != "Confirmed")
                {
                    _logger.LogWarning("Cannot mark {Status} reservation as no-show: {ReservationId}", reservation.Status, id);
                    return false;
                }

                // Record status change
                var oldStatus = reservation.Status;
                reservation.Status = "NoShow";
                reservation.UpdatedAt = DateTime.UtcNow;

                // Add status change record
                var statusChange = new ReservationStatusChange
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    OldStatus = oldStatus,
                    NewStatus = "NoShow",
                    ChangedAt = DateTime.UtcNow,
                    Reason = "Customer did not show up"
                };

                await _reservationRepository.AddStatusChangeAsync(statusChange);

                // Update reservation
                await _reservationRepository.UpdateAsync(reservation);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as no-show: {ReservationId}", id);
                return false;
            }
        }

        public async Task<bool> MarkAsCompletedAsync(Guid id)
        {
            _logger.LogInformation("Marking reservation as completed: {ReservationId}", id);

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found: {ReservationId}", id);
                    return false;
                }

                // Only allow marking confirmed reservations as completed
                if (reservation.Status != "Confirmed")
                {
                    _logger.LogWarning("Cannot mark {Status} reservation as completed: {ReservationId}", reservation.Status, id);
                    return false;
                }

                // Record status change
                var oldStatus = reservation.Status;
                reservation.Status = "Completed";
                reservation.UpdatedAt = DateTime.UtcNow;

                // Add status change record
                var statusChange = new ReservationStatusChange
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    OldStatus = oldStatus,
                    NewStatus = "Completed",
                    ChangedAt = DateTime.UtcNow,
                    Reason = "Reservation completed"
                };

                await _reservationRepository.AddStatusChangeAsync(statusChange);

                // Update reservation
                await _reservationRepository.UpdateAsync(reservation);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as completed: {ReservationId}", id);
                return false;
            }
        }

        public async Task SendReservationRemindersAsync()
        {
            _logger.LogInformation("Processing scheduled reservation reminders");

            try
            {
                await _notificationService.ProcessPendingRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reservation reminders");
                throw;
            }
        }

        #region Helper Methods
        private string GenerateReservationCode()
        {
            // Generate a unique reservation code (e.g., "RSV-ABC123")
            var random = new Random();
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Avoid characters that look similar
            var code = new char[6];

            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }

            return $"R{new string(code)}";
        }

        private async Task<List<ReservationTableInfo>> FindOptimalTableCombinationAsync(
            List<ReservationTableInfo> availableTables,
            int partySize,
            Guid outletId,
            DateTime startTime,
            DateTime endTime)
        {
            // Get tables that are already reserved for this time slot
            var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                outletId, startTime, endTime);

            // Filter out tables that are already reserved
            var trulyAvailableTables = availableTables
                .Where(t => t.IsActive && !reservedTableIds.Contains(t.Id))
                .ToList();

            // First try to find tables that match the party size exactly
            var exactMatchTable = trulyAvailableTables
                .FirstOrDefault(t => t.Capacity == partySize);

            if (exactMatchTable != null)
            {
                return new List<ReservationTableInfo> { exactMatchTable };
            }

            // If no exact match, look for the smallest table that can fit the party
            // (with a reasonable buffer - don't assign a table more than 50% larger than needed)
            var bestFitTable = trulyAvailableTables
                .Where(t => t.Capacity >= partySize && t.Capacity <= partySize * 1.5)
                .OrderBy(t => t.Capacity)
                .FirstOrDefault();

            if (bestFitTable != null)
            {
                return new List<ReservationTableInfo> { bestFitTable };
            }

            // If no table within 50% buffer, look for any table that fits
            var anyFitTable = trulyAvailableTables
                .Where(t => t.Capacity >= partySize)
                .OrderBy(t => t.Capacity)
                .FirstOrDefault();

            if (anyFitTable != null)
            {
                return new List<ReservationTableInfo> { anyFitTable };
            }

            // If we can't find a single table, try to combine tables
            var remainingCapacity = partySize;
            var result = new List<ReservationTableInfo>();

            // Sort tables by capacity in descending order for combining
            var sortedByCapacity = trulyAvailableTables
                .OrderByDescending(t => t.Capacity)
                .ToList();

            foreach (var table in sortedByCapacity)
            {
                if (remainingCapacity <= 0) break;

                result.Add(table);
                remainingCapacity -= table.Capacity;
            }

            // If we couldn't find a combination, return an empty list
            if (remainingCapacity > 0)
            {
                return new List<ReservationTableInfo>();
            }

            return result;
        }

        private async Task ScheduleRemindersAsync(Guid reservationId, DateTime reservationDate)
        {
            try
            {
                // Get the reservation to check existing reminders
                var reservation = await _reservationRepository.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found for scheduling reminders: {ReservationId}", reservationId);
                    return;
                }

                // Schedule confirmation reminder (immediate)
                // Check if it already exists
                if (!reservation.Reminders.Any(r => r.ReminderType == "Confirmation"))
                {
                    var confirmationReminder = new ReservationReminder
                    {
                        Id = Guid.NewGuid(),
                        ReservationId = reservationId,
                        ReminderType = "Confirmation",
                        ScheduledFor = DateTime.UtcNow,
                        Status = "Pending",
                        Channel = "WhatsApp",
                        Content = "Reservation confirmation"
                    };

                    await _reservationRepository.AddReminderAsync(confirmationReminder);
                }

                // Schedule 24-hour reminder
                DateTime reminder24Hour = reservationDate.AddHours(-24);
                if (reminder24Hour > DateTime.UtcNow &&
                    !reservation.Reminders.Any(r => r.ReminderType == "24Hour"))
                {
                    var reminder24 = new ReservationReminder
                    {
                        Id = Guid.NewGuid(),
                        ReservationId = reservationId,
                        ReminderType = "24Hour",
                        ScheduledFor = reminder24Hour,
                        Status = "Pending",
                        Channel = "WhatsApp",
                        Content = "Reservation 24-hour reminder"
                    };

                    await _reservationRepository.AddReminderAsync(reminder24);
                }

                // Schedule 1-hour reminder
                DateTime reminder1Hour = reservationDate.AddHours(-1);
                if (reminder1Hour > DateTime.UtcNow &&
                    !reservation.Reminders.Any(r => r.ReminderType == "1Hour"))
                {
                    var reminder1 = new ReservationReminder
                    {
                        Id = Guid.NewGuid(),
                        ReservationId = reservationId,
                        ReminderType = "1Hour",
                        ScheduledFor = reminder1Hour,
                        Status = "Pending",
                        Channel = "WhatsApp",
                        Content = "Reservation 1-hour reminder"
                    };

                    await _reservationRepository.AddReminderAsync(reminder1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling reminders for reservation: {ReservationId}", reservationId);
                // We'll handle this as a non-fatal error - the reservation is still created
            }
        }

        private async Task<ReservationDto> MapReservationToDto(
            ReservationEntity reservation,
            string outletName,
            IEnumerable<ReservationTableInfo> assignedTables)
        {
            var tableAssignments = assignedTables.Select(t => new TableAssignmentDto
            {
                TableId = t.Id,
                TableNumber = t.TableNumber,
                Section = t.Section,
                Capacity = t.Capacity
            }).ToList();

            return new ReservationDto
            {
                Id = reservation.Id,
                ReservationCode = reservation.ReservationCode,
                OutletId = reservation.OutletId,
                OutletName = outletName,
                CustomerName = reservation.CustomerName,
                CustomerPhone = reservation.CustomerPhone,
                CustomerEmail = reservation.CustomerEmail,
                PartySize = reservation.PartySize,
                ReservationDate = reservation.ReservationDate,
                Duration = reservation.Duration,
                Status = reservation.Status,
                SpecialRequests = reservation.SpecialRequests,
                CreatedAt = reservation.CreatedAt,
                UpdatedAt = reservation.UpdatedAt,
                TableAssignments = tableAssignments
            };
        }
    }
}
        #endregion