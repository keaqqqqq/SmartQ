using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Entities;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using ReservationTableInfo = FNBReservation.Modules.Reservation.Core.Interfaces.TableInfo;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

// Ensure that the ICustomerAdapter interface is defined in the FNBReservation.Modules.Reservation.Core.Adapters namespace
namespace FNBReservation.Modules.Reservation.Infrastructure.Services
{
    public class ReservationService : IReservationService
    {   
        private readonly IReservationRepository _reservationRepository;
        private readonly IReservationNotificationService _notificationService;
        private readonly IOutletAdapter _outletAdapter;
        private readonly ILogger<ReservationService> _logger;
        private readonly ICustomerAdapter _customerAdapter;
        private readonly ITableTypeService _tableTypeService; // Add this

        public ReservationService(
            IReservationRepository reservationRepository,
            IReservationNotificationService notificationService,
            IOutletAdapter outletAdapter,
            ILogger<ReservationService> logger,
            ICustomerAdapter customerAdapter,
            ITableTypeService tableTypeService) // Add this
        {
            _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _outletAdapter = outletAdapter ?? throw new ArgumentNullException(nameof(outletAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _customerAdapter = customerAdapter;
            _tableTypeService = tableTypeService ?? throw new ArgumentNullException(nameof(tableTypeService)); // Add this
        }

        public async Task<TimeSlotAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto request)
        {
            _logger.LogInformation("Checking availability for outlet {OutletId}, party size {PartySize}, date {Date}, preferred time {PreferredTime}",
                request.OutletId, request.PartySize, request.Date, request.PreferredTime);

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

                // Check if requested date is in the past
                DateTime now = DateTime.UtcNow;
                DateTime today = now.Date;

                if (request.Date.Date < today)
                {
                    _logger.LogWarning("Availability check for past date: {Date}", request.Date);
                    throw new ArgumentException("Cannot check availability for past dates");
                }

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

                var (openingTime, closingTime, isOvernight) = ParseOperatingHours(outlet.OperatingHours);

                // Log the parsed operating hours for debugging
                _logger.LogInformation("Parsed operating hours: {OpeningTime} to {ClosingTime}, Overnight: {IsOvernight}",
                    FormatTimeSpan(openingTime), FormatTimeSpan(closingTime), isOvernight);

                // If the preferred time is not provided, use opening time
                TimeSpan preferredTime = request.PreferredTime ?? openingTime;
                DateTime exactPreferredDateTime = request.Date.Date.Add(preferredTime);

                // Check if the preferred time is within operating hours using the improved method
                if (!IsWithinOperatingHours(preferredTime, openingTime, closingTime, isOvernight))
                {
                    string formattedOpeningTime = FormatTimeSpan(openingTime);
                    string formattedClosingTime = FormatTimeSpan(closingTime);

                    _logger.LogWarning("Preferred time {PreferredTime} is outside operating hours ({OpeningTime} - {ClosingTime})",
                        FormatTimeSpan(preferredTime), formattedOpeningTime, formattedClosingTime);
                    throw new ArgumentException($"The selected time is outside our operating hours ({formattedOpeningTime} - {formattedClosingTime})");
                }

                // First check availability at the preferred time
                var preferredSettings = await _outletAdapter.GetReservationSettingsAsync(request.OutletId, exactPreferredDateTime);
                if (preferredSettings == null)
                {
                    _logger.LogWarning("Could not get reservation settings for outlet {OutletId} at {DateTime}",
                        request.OutletId, exactPreferredDateTime);
                    throw new InvalidOperationException("Could not retrieve reservation settings");
                }

                // If reservation allocation is 0% at preferred time, it's not available
                if (preferredSettings.ReservationAllocationPercent <= 0)
                {
                    _logger.LogInformation("Preferred time {TimeSlot} not available as reservation allocation is {Allocation}%",
                        exactPreferredDateTime, preferredSettings.ReservationAllocationPercent);
                }
                else
                {
                    // Calculate the end time for this reservation based on the dining duration
                    DateTime endTime = exactPreferredDateTime.AddMinutes(preferredSettings.DefaultDiningDurationMinutes);

                    // Get all tables for this outlet
                    var allTables = await _outletAdapter.GetTablesAsync(request.OutletId);

                    // Get tables that are already reserved for this time slot
                    var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                        request.OutletId, exactPreferredDateTime, endTime);

                    // Get reserved and available tables
                    var reservedTables = allTables.Where(t => reservedTableIds.Contains(t.Id)).ToList();
                    var availableTables = allTables.Where(t => t.IsActive && !reservedTableIds.Contains(t.Id)).ToList();

                    // Calculate the total capacity of all active tables
                    int totalCapacity = allTables.Where(t => t.IsActive).Sum(t => t.Capacity);

                    // Calculate the capacity of tables already reserved (this is the key fix)
                    int reservedCapacity = reservedTables.Sum(t => t.Capacity);

                    var reservationTables = await _tableTypeService.GetReservationTablesAsync(request.OutletId, exactPreferredDateTime);
                    int maxReservationCapacity = reservationTables.Sum(t => t.Capacity);

                    // Calculate the remaining capacity for reservations
                    int remainingReservationCapacity = maxReservationCapacity - reservedCapacity;

                    _logger.LogInformation("Time slot {DateTime}: Total capacity: {Total}, Reserved capacity: {Reserved}, " +
                        "Max reservation capacity: {Max}, Remaining capacity: {Remaining}",
                        exactPreferredDateTime, totalCapacity, reservedCapacity, maxReservationCapacity, remainingReservationCapacity);

                    // Check if the party can be accommodated within the remaining capacity
                    if (request.PartySize <= remainingReservationCapacity)
                    {
                        // Now we need to find suitable tables that won't exceed the remaining capacity
                        bool hasSuitableTables = false;

                        // First try exact match table (no wasted capacity)
                        var exactMatchTable = availableTables.FirstOrDefault(t => t.Capacity == request.PartySize);
                        if (exactMatchTable != null)
                        {
                            hasSuitableTables = true;
                            _logger.LogInformation("Found exact match table for party size {PartySize}", request.PartySize);
                        }
                        else
                        {
                            // Try to find the smallest table that can fit the party without exceeding the limit
                            var suitableTables = availableTables
                                .Where(t => t.Capacity >= request.PartySize && t.Capacity <= remainingReservationCapacity)
                                .OrderBy(t => t.Capacity)
                                .ToList();

                            if (suitableTables.Any())
                            {
                                hasSuitableTables = true;
                                _logger.LogInformation("Found table with capacity {Capacity} for party size {PartySize}",
                                    suitableTables.First().Capacity, request.PartySize);
                            }
                            else
                            {
                                // Try combining smaller tables
                                var smallerTables = availableTables
                                    .Where(t => t.Capacity < request.PartySize)
                                    .OrderByDescending(t => t.Capacity)
                                    .ToList();

                                if (smallerTables.Any())
                                {
                                    var selectedTables = new List<ReservationTableInfo>();
                                    int currentCapacity = 0;
                                    int remainingPartySize = request.PartySize;

                                    foreach (var table in smallerTables)
                                    {
                                        if (remainingPartySize <= 0) break;

                                        // Check if adding this table would exceed the remaining allocation
                                        if (currentCapacity + table.Capacity > remainingReservationCapacity)
                                            continue;

                                        selectedTables.Add(table);
                                        currentCapacity += table.Capacity;
                                        remainingPartySize -= table.Capacity;
                                    }

                                    hasSuitableTables = remainingPartySize <= 0;

                                    if (hasSuitableTables)
                                    {
                                        _logger.LogInformation("Found combination of {Count} tables for party size {PartySize}",
                                            selectedTables.Count, request.PartySize);
                                    }
                                }
                            }
                        }

                        if (hasSuitableTables)
                        {
                            // Only add as available if we can actually accommodate this party
                            response.AvailableTimeSlots.Add(new AvailableTimeslotDto
                            {
                                DateTime = exactPreferredDateTime,
                                AvailableCapacity = remainingReservationCapacity,
                                IsPreferred = true
                            });
                        }
                    }
                }

                // If preferred time is not available, find alternatives
                if (response.AvailableTimeSlots.Count == 0)
                {
                    // Generate alternative time slots at 15-minute intervals within 2 hours before and after the preferred time
                    TimeSpan interval = TimeSpan.FromMinutes(15);
                    TimeSpan window = TimeSpan.FromHours(2);

                    DateTime startSearchTime = exactPreferredDateTime.Add(-window);
                    if (startSearchTime.TimeOfDay < openingTime)
                        startSearchTime = request.Date.Date.Add(openingTime);

                    DateTime endSearchTime = exactPreferredDateTime.Add(window);
                    if (endSearchTime.TimeOfDay > closingTime)
                        endSearchTime = request.Date.Date.Add(closingTime);

                    // Check each time slot around the preferred time
                    for (DateTime slotTime = startSearchTime; slotTime <= endSearchTime; slotTime = slotTime.Add(interval))
                    {
                        // Skip the preferred time as we've already checked it
                        if (slotTime == exactPreferredDateTime)
                            continue;

                        // For each time slot, check availability based on outlet's settings for that time
                        var settings = await _outletAdapter.GetReservationSettingsAsync(request.OutletId, slotTime);

                        if (settings == null || settings.ReservationAllocationPercent <= 0)
                            continue;

                        // Calculate the end time for this reservation based on the dining duration
                        DateTime endTime = slotTime.AddMinutes(settings.DefaultDiningDurationMinutes);

                        // Get tables designated for reservations at this time slot
                        var reservationTables = await _tableTypeService.GetReservationTablesAsync(request.OutletId, slotTime);

                        if (!reservationTables.Any())
                        {
                            _logger.LogInformation("No tables designated for reservations at time {DateTime} for outlet {OutletId}",
                                slotTime, request.OutletId);
                            continue;
                        }

                        // Get already reserved tables for this time slot
                        var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                            request.OutletId, slotTime, endTime);

                        // Get held tables
                        var heldTableIds = await _reservationRepository.GetHeldTableIdsForTimeSlotAsync(
                            request.OutletId, slotTime, endTime);

                        // Combine reserved and held tables to get all unavailable tables
                        var unavailableTableIds = reservedTableIds.Concat(heldTableIds).Distinct().ToList();

                        // Get available reservation tables
                        var availableReservationTables = reservationTables
                            .Where(t => t.IsActive && !unavailableTableIds.Contains(t.Id))
                            .ToList();

                        // Calculate the total available capacity for informational purposes
                        int availableCapacity = availableReservationTables.Sum(t => t.Capacity);

                        // Check if we can accommodate this party size with the available tables
                        bool hasSuitableTables = false;

                        // First try exact match table (no wasted capacity)
                        if (availableReservationTables.Any(t => t.Capacity == request.PartySize))
                        {
                            hasSuitableTables = true;
                        }
                        else if (availableReservationTables.Any(t => t.Capacity >= request.PartySize))
                        {
                            // Try to find a single table that can accommodate the party
                            hasSuitableTables = true;
                        }
                        else
                        {
                            // Try combining smaller tables
                            var smallerTables = availableReservationTables
                                .Where(t => t.Capacity < request.PartySize)
                                .OrderByDescending(t => t.Capacity)
                                .ToList();

                            if (smallerTables.Any())
                            {
                                int combinedCapacity = 0;

                                foreach (var table in smallerTables)
                                {
                                    combinedCapacity += table.Capacity;
                                    if (combinedCapacity >= request.PartySize)
                                    {
                                        hasSuitableTables = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (hasSuitableTables)
                        {
                            // Only add as available if we can actually accommodate this party
                            response.AlternativeTimeSlots.Add(new AvailableTimeslotDto
                            {
                                DateTime = slotTime,
                                AvailableCapacity = availableCapacity, // Using actual table capacity now
                                IsPreferred = false
                            });
                        }
                    }

                    // Sort alternative time slots by how close they are to the preferred time
                    response.AlternativeTimeSlots = response.AlternativeTimeSlots
                        .OrderBy(ts => Math.Abs((ts.DateTime - exactPreferredDateTime).TotalMinutes))
                        .ToList();
                }

                _logger.LogInformation("Preferred time available: {IsAvailable}, alternative slots found: {AlternativeCount}",
                    response.AvailableTimeSlots.Count > 0, response.AlternativeTimeSlots.Count);

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

                // Verify the hold if HoldId is provided
                List<ReservationTableInfo> tablesToAssign = new List<ReservationTableInfo>();

                if (createReservationDto.HoldId != Guid.Empty)
                {
                    var hold = await _reservationRepository.GetTableHoldByIdAsync(createReservationDto.HoldId);

                    if (hold == null || !hold.IsActive)
                    {
                        throw new InvalidOperationException("The table hold has expired or is invalid. Please start again.");
                    }

                    if (hold.SessionId != createReservationDto.SessionId)
                    {
                        throw new InvalidOperationException("This hold belongs to another session.");
                    }

                    if (DateTime.UtcNow > hold.HoldExpiresAt)
                    {
                        throw new InvalidOperationException("The table hold has expired. Please start again.");
                    }

                    // Use the tables from the hold
                    var availableTables = await _outletAdapter.GetTablesAsync(createReservationDto.OutletId);
                    tablesToAssign = availableTables
                        .Where(t => hold.TableIds.Contains(t.Id))
                        .ToList();

                    // Verify that the hold tables are still valid reservation tables
                    var reservationTables = await _tableTypeService.GetReservationTablesAsync(
                        createReservationDto.OutletId,
                        createReservationDto.ReservationDate);

                    var reservationTableIds = reservationTables.Select(t => t.Id).ToHashSet();

                    // Check if all held tables are still designated as reservation tables
                    if (hold.TableIds.Any(id => !reservationTableIds.Contains(id)))
                    {
                        _logger.LogWarning("Some held tables are no longer designated as reservation tables");
                        throw new InvalidOperationException("The reservation tables have changed. Please try again.");
                    }

                    // Check if the tables are still free (not already reserved)
                    var holdSettings = await _outletAdapter.GetReservationSettingsAsync(
                        createReservationDto.OutletId,
                        createReservationDto.ReservationDate);

                    if (holdSettings == null)
                    {
                        _logger.LogWarning("Could not get reservation settings for outlet {OutletId}", createReservationDto.OutletId);
                        throw new InvalidOperationException("Could not retrieve reservation settings");
                    }

                    // Get already reserved tables for this time slot
                    DateTime endTime = createReservationDto.ReservationDate.AddMinutes(holdSettings.DefaultDiningDurationMinutes);
                    var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                        createReservationDto.OutletId,
                        createReservationDto.ReservationDate,
                        endTime);

                    // Check if any of the held tables are now reserved by someone else
                    if (hold.TableIds.Any(id => reservedTableIds.Contains(id)))
                    {
                        _logger.LogWarning("Some held tables are already reserved by others");
                        throw new InvalidOperationException("Some tables are no longer available. Please try again or select a different time.");
                    }

                    // Mark the hold as used
                    await _reservationRepository.ReleaseTableHoldAsync(hold.Id);
                }

                // Check reservation is within allowed time range
                DateTime now = DateTime.UtcNow;
                DateTime today = now.Date;

                if (createReservationDto.ReservationDate.Date < today)
                {
                    _logger.LogWarning("Reservation date is in the past: {ReservationDate}", createReservationDto.ReservationDate);
                    throw new ArgumentException("Reservations cannot be made for past dates");
                }

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

                // Check if there's capacity for this reservation (skip if we already have a table hold)
                if (tablesToAssign.Count == 0)
                {
                    // Get tables designated for reservations at this time
                    var reservationTables = await _tableTypeService.GetReservationTablesAsync(
                        createReservationDto.OutletId,
                        createReservationDto.ReservationDate);

                    if (!reservationTables.Any())
                    {
                        _logger.LogWarning("No tables designated for reservations at this time");
                        throw new InvalidOperationException("No tables are available for reservations at this time.");
                    }

                    // Get already reserved tables for this time slot
                    DateTime endTime = createReservationDto.ReservationDate.Add(duration);
                    var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                        createReservationDto.OutletId,
                        createReservationDto.ReservationDate,
                        endTime);

                    // Get held tables (excluding tables being reserved right now)
                    var heldTableIds = await _reservationRepository.GetHeldTableIdsForTimeSlotAsync(
                        createReservationDto.OutletId,
                        createReservationDto.ReservationDate,
                        endTime,
                        createReservationDto.SessionId);

                    // Combine reserved and held tables to get all unavailable tables
                    var unavailableTableIds = reservedTableIds.Concat(heldTableIds).Distinct().ToList();

                    // Convert TableDto to ReservationTableInfo for available tables
                    var availableReservationTables = reservationTables
                        .Where(t => t.IsActive && !unavailableTableIds.Contains(t.Id))
                        .Select(t => new ReservationTableInfo
                        {
                            Id = t.Id,
                            TableNumber = t.TableNumber,
                            Capacity = t.Capacity,
                            Section = t.Section,
                            IsActive = t.IsActive
                        })
                        .ToList();

                    _logger.LogInformation("Found {Count} available reservation tables for outlet {OutletId} at {DateTime}",
                        availableReservationTables.Count, createReservationDto.OutletId, createReservationDto.ReservationDate);

                    // Find optimal combination of tables for this party size
                    tablesToAssign = await FindOptimalTableCombinationFromAvailableTablesAsync(
                        availableReservationTables,
                        createReservationDto.PartySize);

                    // Check if we found suitable tables
                    if (tablesToAssign == null || tablesToAssign.Count == 0)
                    {
                        _logger.LogWarning("No suitable reservation tables available for party size {PartySize}",
                            createReservationDto.PartySize);
                        throw new InvalidOperationException("No suitable tables available for this reservation. Please try a different time or party size.");
                    }
                }

                Guid customerId = await _customerAdapter.GetOrCreateCustomerAsync(
                createReservationDto.CustomerName,
                createReservationDto.CustomerPhone,
                createReservationDto.CustomerEmail);

                _logger.LogInformation("Reservation associated with customer ID: {CustomerId}", customerId);

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

                // Track total capacity of assigned tables to verify we're not exceeding limits
                int totalAssignedCapacity = 0;

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
                    totalAssignedCapacity += table.Capacity;
                }

                // Log the total assigned capacity vs party size for monitoring
                _logger.LogInformation("Reservation {Id} created with party size {PartySize} and assigned table capacity {TableCapacity}",
                    createdReservation.Id, createReservationDto.PartySize, totalAssignedCapacity);

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

                // Schedule initial confirmation reminder
                await ScheduleInitialConfirmationReminderAsync(createdReservation.Id);

                // Schedule other reminders
                await ScheduleRemindersAsync(createdReservation.Id, createdReservation.ReservationDate);

                // Send confirmation notification
                //await _notificationService.SendConfirmationAsync(createdReservation.Id);

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

                // Check if reservation is too close to start time
                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", reservation.OutletId);
                    throw new InvalidOperationException("Cannot update reservation: outlet not found");
                }

                // Calculate the minimum time allowed for modifications
                int modificationCutoffHours = Math.Max(2, outlet.MinAdvanceReservationTime / 2);
                DateTime cutoffTime = reservation.ReservationDate.AddHours(-modificationCutoffHours);

                if (DateTime.UtcNow > cutoffTime)
                {
                    _logger.LogWarning("Reservation too close to start time for updates: {ReservationId}", id);
                    throw new InvalidOperationException($"Reservations cannot be updated within {modificationCutoffHours} hours of the reservation time");
                }

                // Track changes for notification
                List<string> changes = new List<string>();

                // Check if we're attempting to change party size or date/time
                bool isChangingTableAllocation = updateReservationDto.PartySize.HasValue || updateReservationDto.ReservationDate.HasValue;

                // Verify hold if changing table allocation
                TableHold hold = null;
                if (isChangingTableAllocation)
                {
                    // If changing party size or date/time, we must have a hold
                    if (!updateReservationDto.HoldId.HasValue || string.IsNullOrEmpty(updateReservationDto.SessionId))
                    {
                        _logger.LogWarning("Attempt to change party size or date/time without a hold: {ReservationId}", id);
                        throw new InvalidOperationException("Changing party size or reservation time requires a table hold");
                    }

                    // Verify the hold
                    hold = await _reservationRepository.GetTableHoldByIdAsync(updateReservationDto.HoldId.Value);

                    if (hold == null || !hold.IsActive)
                    {
                        throw new InvalidOperationException("The table hold has expired or is invalid. Please start again.");
                    }

                    if (hold.SessionId != updateReservationDto.SessionId)
                    {
                        throw new InvalidOperationException("This hold belongs to another session.");
                    }

                    if (DateTime.UtcNow > hold.HoldExpiresAt)
                    {
                        throw new InvalidOperationException("The table hold has expired. Please start again.");
                    }
                }

                // Update basic fields
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

                // Handle party size and date/time updates if a valid hold is provided
                if (isChangingTableAllocation && hold != null)
                {
                    // Update party size
                    if (updateReservationDto.PartySize.HasValue && updateReservationDto.PartySize.Value != reservation.PartySize)
                    {
                        changes.Add($"Party size updated from {reservation.PartySize} to {updateReservationDto.PartySize.Value}");
                        reservation.PartySize = updateReservationDto.PartySize.Value;
                    }

                    // Update reservation date/time
                    if (updateReservationDto.ReservationDate.HasValue && updateReservationDto.ReservationDate.Value != reservation.ReservationDate)
                    {
                        changes.Add($"Date/time updated from {reservation.ReservationDate} to {updateReservationDto.ReservationDate.Value}");
                        reservation.ReservationDate = updateReservationDto.ReservationDate.Value;

                        // Update duration based on settings
                        var settings = await _outletAdapter.GetReservationSettingsAsync(
                            reservation.OutletId, reservation.ReservationDate);

                        if (settings != null)
                        {
                            reservation.Duration = TimeSpan.FromMinutes(settings.DefaultDiningDurationMinutes);
                        }

                        // Reschedule reminders
                        await ScheduleRemindersAsync(reservation.Id, reservation.ReservationDate);
                    }

                    // Clear current table assignments
                    foreach (var assignment in reservation.TableAssignments.ToList())
                    {
                        await _reservationRepository.RemoveTableAssignmentAsync(reservation.Id, assignment.TableId);
                    }

                    // Assign tables from the hold
                    var availableTables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var tablesToAssign = availableTables
                        .Where(t => hold.TableIds.Contains(t.Id))
                        .ToList();

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

                    // Release the hold
                    await _reservationRepository.ReleaseTableHoldAsync(hold.Id);
                }

                if (changes.Any())
                {
                    // Update reservation
                    reservation.UpdatedAt = DateTime.UtcNow;
                    var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

                    // Notify customer of changes
                    await _notificationService.SendModificationAsync(reservation.Id, string.Join(", ", changes));

                    // Return updated reservation
                    var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var assignedTables = tables
                        .Where(t => updatedReservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();

                    return await MapReservationToDto(updatedReservation, outlet?.Name ?? "Unknown Outlet", assignedTables);
                }
                else
                {
                    _logger.LogInformation("No changes detected for reservation: {ReservationId}", id);

                    // Get updated information even if no changes
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

                // Check if the reservation is too close to start time for cancellation
                var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found for reservation: {ReservationId}", id);
                    throw new InvalidOperationException("Outlet information not available");
                }

                // Calculate the minimum time allowed for cancellation
                // Use a minimum of 2 hours or half of the outlet's minimum advance reservation time
                int cancellationCutoffHours = Math.Max(2, outlet.MinAdvanceReservationTime / 2);
                DateTime cutoffTime = reservation.ReservationDate.AddHours(-cancellationCutoffHours);

                if (DateTime.UtcNow > cutoffTime)
                {
                    _logger.LogWarning("Reservation too close to start time for cancellation: {ReservationId}, starts at {StartTime}, cutoff was {CutoffTime}",
                        id, reservation.ReservationDate, cutoffTime);
                    throw new InvalidOperationException($"Reservations cannot be canceled within {cancellationCutoffHours} hours of the reservation time");
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

        public async Task<TableHoldResponseDto> HoldTablesForReservationAsync(TableHoldRequestDto request)
        {
            _logger.LogInformation("Attempting to hold tables for outlet {OutletId}, party size {PartySize}, date {DateTime}",
                request.OutletId, request.PartySize, request.ReservationDateTime);

            try
            {
                // Validate outlet exists and is active
                var outlet = await _outletAdapter.GetOutletInfoAsync(request.OutletId);
                if (outlet == null || !outlet.IsActive)
                {
                    throw new ArgumentException($"Outlet with ID {request.OutletId} not found or inactive");
                }

                // Check if there's an existing hold for this session
                var existingHold = await _reservationRepository.GetTableHoldBySessionIdAsync(request.SessionId);
                if (existingHold != null && existingHold.IsActive)
                {
                    // Release the previous hold
                    await _reservationRepository.ReleaseTableHoldAsync(existingHold.Id);
                }

                // Get reservation settings for the time slot
                var settings = await _outletAdapter.GetReservationSettingsAsync(request.OutletId, request.ReservationDateTime);
                if (settings == null || settings.ReservationAllocationPercent <= 0)
                {
                    throw new InvalidOperationException("This time slot is not available for reservations");
                }

                // Calculate end time based on dining duration
                DateTime endTime = request.ReservationDateTime.AddMinutes(settings.DefaultDiningDurationMinutes);

                // Find optimal table combination considering both existing reservations and active holds
                var availableTables = await _outletAdapter.GetTablesAsync(request.OutletId);
                var tablesToAssign = await FindOptimalTableCombinationWithHoldsAsync(
                    availableTables.ToList(),
                    request.PartySize,
                    request.OutletId,
                    request.ReservationDateTime,
                    endTime,
                    request.SessionId // We'll ignore this session's own holds
                );

                if (tablesToAssign == null || tablesToAssign.Count == 0)
                {
                    throw new InvalidOperationException("No suitable tables available for this reservation");
                }

                // Create a table hold
                var tableHold = new TableHold
                {
                    Id = Guid.NewGuid(),
                    OutletId = request.OutletId,
                    TableIds = tablesToAssign.Select(t => t.Id).ToList(),
                    PartySize = request.PartySize,
                    ReservationDateTime = request.ReservationDateTime,
                    HoldCreatedAt = DateTime.UtcNow,
                    HoldExpiresAt = DateTime.UtcNow.AddMinutes(3), // 3-minute hold
                    SessionId = request.SessionId,
                    IsActive = true
                };

                var createdHold = await _reservationRepository.CreateTableHoldAsync(tableHold);

                // Return response with hold details and expiration time
                return new TableHoldResponseDto
                {
                    HoldId = createdHold.Id,
                    OutletId = createdHold.OutletId,
                    ReservationDateTime = createdHold.ReservationDateTime,
                    ExpiresAt = createdHold.HoldExpiresAt,
                    IsSuccessful = true,
                    TableNumbers = tablesToAssign.Select(t => t.TableNumber).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error holding tables for reservation");

                return new TableHoldResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> ReleaseTableHoldAsync(Guid holdId)
        {
            _logger.LogInformation("Releasing table hold: {HoldId}", holdId);

            try
            {
                return await _reservationRepository.ReleaseTableHoldAsync(holdId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing table hold: {HoldId}", holdId);
                return false;
            }
        }

        public async Task<TableHoldResponseDto> UpdateTableHoldTimeAsync(UpdateHoldTimeRequestDto request)
        {
            _logger.LogInformation("Updating table hold {HoldId} to new time {NewTime}",
                request.HoldId, request.NewReservationDateTime);

            try
            {
                // Verify the original hold exists and is valid
                var originalHold = await _reservationRepository.GetTableHoldByIdAsync(request.HoldId);

                if (originalHold == null || !originalHold.IsActive)
                {
                    return new TableHoldResponseDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "The original table hold has expired or is invalid."
                    };
                }

                if (originalHold.SessionId != request.SessionId)
                {
                    return new TableHoldResponseDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "This hold belongs to another session."
                    };
                }

                if (DateTime.UtcNow > originalHold.HoldExpiresAt)
                {
                    return new TableHoldResponseDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "The original table hold has expired."
                    };
                }

                // Check if the new time is available
                var settings = await _outletAdapter.GetReservationSettingsAsync(request.OutletId, request.NewReservationDateTime);
                if (settings == null || settings.ReservationAllocationPercent <= 0)
                {
                    return new TableHoldResponseDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "The selected time is not available for reservations."
                    };
                }

                // Release the old hold
                await _reservationRepository.ReleaseTableHoldAsync(originalHold.Id);

                // Create a new hold for the new time
                var availableTables = await _outletAdapter.GetTablesAsync(request.OutletId);
                var endTime = request.NewReservationDateTime.AddMinutes(settings.DefaultDiningDurationMinutes);

                var tablesToAssign = await FindOptimalTableCombinationWithHoldsAsync(
                    availableTables.ToList(),
                    request.PartySize,
                    request.OutletId,
                    request.NewReservationDateTime,
                    endTime,
                    request.SessionId
                );

                if (tablesToAssign == null || tablesToAssign.Count == 0)
                {
                    return new TableHoldResponseDto
                    {
                        IsSuccessful = false,
                        ErrorMessage = "No suitable tables available for the new time."
                    };
                }

                // Create a new table hold
                var tableHold = new TableHold
                {
                    Id = Guid.NewGuid(),
                    OutletId = request.OutletId,
                    TableIds = tablesToAssign.Select(t => t.Id).ToList(),
                    PartySize = request.PartySize,
                    ReservationDateTime = request.NewReservationDateTime,
                    HoldCreatedAt = DateTime.UtcNow,
                    HoldExpiresAt = originalHold.HoldExpiresAt, // Keep the original expiration time
                    SessionId = request.SessionId,
                    IsActive = true
                };

                var createdHold = await _reservationRepository.CreateTableHoldAsync(tableHold);

                // Return response with hold details
                return new TableHoldResponseDto
                {
                    HoldId = createdHold.Id,
                    OutletId = createdHold.OutletId,
                    ReservationDateTime = createdHold.ReservationDateTime,
                    ExpiresAt = createdHold.HoldExpiresAt,
                    IsSuccessful = true,
                    TableNumbers = tablesToAssign.Select(t => t.TableNumber).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table hold time: {Message}", ex.Message);

                return new TableHoldResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "An error occurred while updating the hold time: " + ex.Message
                };
            }
        }

        public async Task<List<TimeSlotDto>> GetAlternativeTimeSlotsAsync(
       Guid outletId, DateTime referenceTime, int partySize, int rangeMinutes = 30)
        {
            _logger.LogInformation("Getting alternative time slots around {ReferenceTime} for outlet {OutletId}",
                referenceTime, outletId);

            try
            {
                // Validate outlet
                var outlet = await _outletAdapter.GetOutletInfoAsync(outletId);
                if (outlet == null || !outlet.IsActive)
                {
                    throw new ArgumentException($"Outlet with ID {outletId} not found or inactive");
                }

                // Define the time range to search
                DateTime startTime = referenceTime.AddMinutes(-rangeMinutes);
                DateTime endTime = referenceTime.AddMinutes(rangeMinutes);

                // Get time slots at 15-minute intervals
                TimeSpan interval = TimeSpan.FromMinutes(15);

                var timeSlots = new List<TimeSlotDto>();

                // Check each time slot
                for (DateTime slotTime = startTime; slotTime <= endTime; slotTime = slotTime.Add(interval))
                {
                    // Skip if it's the reference time
                    if (slotTime == referenceTime)
                        continue;

                    // Get settings for this time
                    var settings = await _outletAdapter.GetReservationSettingsAsync(outletId, slotTime);

                    if (settings == null || settings.ReservationAllocationPercent <= 0)
                        continue;

                    // Calculate the end time for this reservation
                    DateTime slotEndTime = slotTime.AddMinutes(settings.DefaultDiningDurationMinutes);

                    // Get tables designated for reservations at this time
                    var reservationTables = await _tableTypeService.GetReservationTablesAsync(outletId, slotTime);

                    if (!reservationTables.Any())
                    {
                        _logger.LogInformation("No tables designated for reservations at time {DateTime} for outlet {OutletId}",
                            slotTime, outletId);
                        continue;
                    }

                    // Get already reserved tables for this time slot
                    var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                        outletId, slotTime, slotEndTime);

                    // Get held tables
                    var heldTableIds = await _reservationRepository.GetHeldTableIdsForTimeSlotAsync(
                        outletId, slotTime, slotEndTime);

                    // Combine reserved and held tables to get all unavailable tables
                    var unavailableTableIds = reservedTableIds.Concat(heldTableIds).Distinct().ToList();

                    // Get available reservation tables
                    var availableReservationTables = reservationTables
                        .Where(t => t.IsActive && !unavailableTableIds.Contains(t.Id))
                        .ToList();

                    // Check if we can accommodate this party size with the available tables
                    bool canAccommodate = CanAccommodatePartySize(availableReservationTables, partySize);

                    if (canAccommodate)
                    {
                        // Calculate the total available capacity for informational purposes
                        int availableCapacity = availableReservationTables.Sum(t => t.Capacity);

                        timeSlots.Add(new TimeSlotDto
                        {
                            DateTime = slotTime,
                            AvailableCapacity = availableCapacity,
                            IsAvailable = true
                        });
                    }
                }

                // Sort by how close they are to the reference time
                return timeSlots
                    .OrderBy(ts => Math.Abs((ts.DateTime - referenceTime).TotalMinutes))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alternative time slots");
                throw;
            }
        }

        // In ReservationService.cs
        public async Task<List<TimeSlotDto>> GetAlternativeTimesForHoldAsync(Guid holdId)
        {
            _logger.LogInformation("Getting alternative times for hold: {HoldId}", holdId);

            // Get the hold details
            var hold = await _reservationRepository.GetTableHoldByIdAsync(holdId);

            if (hold == null || !hold.IsActive)
            {
                throw new ArgumentException("Hold not found or has expired");
            }

            // Get alternative times around the hold's reservation time (±30 minutes)
            return await GetAlternativeTimeSlotsAsync(
                hold.OutletId,
                hold.ReservationDateTime,
                hold.PartySize,
                30); // 30 minutes before and after
        }
        public async Task<ReservationSearchResultDto> SearchReservationsAsync(
            List<Guid> outletIds,
            string searchTerm,
            List<string> statuses,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize,
            bool isAdmin)
        {
            _logger.LogInformation("Searching reservations with filters: Outlets: {OutletIds}, Search: {SearchTerm}, " +
                "Statuses: {Statuses}, Date Range: {StartDate} to {EndDate}, Page: {Page}",
                outletIds != null ? string.Join(",", outletIds) : "All",
                searchTerm,
                statuses != null ? string.Join(",", statuses) : "All",
                startDate,
                endDate,
                page);

            try
            {
                // Default values
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                // Get raw search results from repository
                var (reservations, totalCount) = await _reservationRepository.SearchReservationsAsync(
                    outletIds,
                    searchTerm,
                    statuses,
                    startDate,
                    endDate,
                    page,
                    pageSize);

                // Calculate total pages
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Convert entities to DTOs
                var reservationDtos = new List<ReservationDto>();

                foreach (var reservation in reservations)
                {
                    // Get outlet info for each reservation
                    var outlet = await _outletAdapter.GetOutletInfoAsync(reservation.OutletId);
                    string outletName = outlet?.Name ?? "Unknown Outlet";

                    // Get assigned tables
                    var tables = await _outletAdapter.GetTablesAsync(reservation.OutletId);
                    var assignedTables = tables
                        .Where(t => reservation.TableAssignments.Any(ta => ta.TableId == t.Id))
                        .ToList();

                    reservationDtos.Add(await MapReservationToDto(reservation, outletName, assignedTables));
                }

                // Prepare response
                return new ReservationSearchResultDto
                {
                    Reservations = reservationDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    SearchTerm = searchTerm,
                    AppliedStatuses = statuses ?? new List<string>(),
                    AppliedOutletIds = outletIds ?? new List<Guid>(),
                    StartDate = startDate,
                    EndDate = endDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching reservations");
                throw;
            }
        }

        private async Task<List<ReservationTableInfo>> FindOptimalTableCombinationWithHoldsAsync(
         List<ReservationTableInfo> availableTables,
         int partySize,
         Guid outletId,
         DateTime startTime,
         DateTime endTime,
         string currentSessionId)
        {
            // Get reservation settings to determine limits
            var settings = await _outletAdapter.GetReservationSettingsAsync(outletId, startTime);
            if (settings == null)
            {
                _logger.LogWarning("Could not get reservation settings for outlet {OutletId}", outletId);
                return new List<ReservationTableInfo>();
            }

            var reservationTablesInfo = new List<ReservationTableInfo>();
            foreach (var table in availableTables)
            {
                bool isReservationTable = await _tableTypeService.IsReservationTableAsync(outletId, table.Id, startTime);
                if (isReservationTable)
                {
                    reservationTablesInfo.Add(table);
                }
            }

            // If no reservation tables are available, log warning and continue with standard logic
            // This is a fallback in case the system is misconfigured or all tables are assigned to queue
            if (!reservationTablesInfo.Any())
            {
                _logger.LogWarning("No reservation tables found for outlet {OutletId} at {StartTime}. Using all available tables.",
                    outletId, startTime);
                reservationTablesInfo = availableTables;
            }

            // Get tables that are already reserved for this time slot
            var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                outletId, startTime, endTime);

            // Get tables that are currently on hold (except for the current session)
            var heldTableIds = await _reservationRepository.GetHeldTableIdsForTimeSlotAsync(
                outletId, startTime, endTime, currentSessionId);

            // Combine reserved and held table IDs
            var unavailableTableIds = reservedTableIds.Concat(heldTableIds).Distinct().ToList();

            // Filter out tables that are already reserved or on hold
            var trulyAvailableTables = reservationTablesInfo
                .Where(t => t.IsActive && !unavailableTableIds.Contains(t.Id))
                .ToList();

            // Get current reservation capacity usage
            int reservedCapacity = await _reservationRepository.GetReservedCapacityForTimeSlotAsync(
                outletId, startTime, endTime);

            // Get reservation tables directly from TableTypeService like in CheckAvailabilityAsync:
            var allReservationTables = await _tableTypeService.GetReservationTablesAsync(outletId, startTime);
            int maxReservationCapacity = allReservationTables.Sum(t => t.Capacity);

            // Calculate the remaining capacity
            int remainingReservationCapacity = maxReservationCapacity - reservedCapacity;

            // If the party size exceeds the remaining reservation capacity, fail
            if (partySize > remainingReservationCapacity)
            {
                _logger.LogWarning("Party size {PartySize} exceeds remaining reservation capacity {RemainingCapacity}",
                    partySize, remainingReservationCapacity);
                return new List<ReservationTableInfo>();
            }

            _logger.LogInformation("Searching for table(s) for party size {PartySize}, remaining capacity {RemainingCapacity}",
                partySize, remainingReservationCapacity);

            // CRITICAL FIX: The key issue is that we need to account for table capacity waste
            // If there's a 50% allocation limit, a 3-person party shouldn't get an 8-person table
            // as that would effectively use 8 seats from the allocated capacity

            // Strategy 1: Look for exact match first (best case - no waste)
            var exactMatchTable = trulyAvailableTables
                .FirstOrDefault(t => t.Capacity == partySize);

            if (exactMatchTable != null)
            {
                _logger.LogInformation("Found exact match table {TableNumber} with capacity {Capacity} for party size {PartySize}",
                    exactMatchTable.TableNumber, exactMatchTable.Capacity, partySize);
                return new List<ReservationTableInfo> { exactMatchTable };
            }

            // Strategy 2: Find a table that can accommodate the party WITHOUT exceeding allocation
            // Calculate the maximum table capacity we can use without exceeding the allocation
            int maxAllowableCapacity = remainingReservationCapacity;

            // Find tables that can accommodate the party and don't exceed the allocation
            var suitableTables = trulyAvailableTables
                .Where(t => t.Capacity >= partySize && t.Capacity <= maxAllowableCapacity)
                .OrderBy(t => t.Capacity)  // Get the smallest suitable table
                .ToList();

            if (suitableTables.Any())
            {
                var bestFitTable = suitableTables.First();
                _logger.LogInformation("Found suitable table {TableNumber} with capacity {Capacity} for party size {PartySize}",
                    bestFitTable.TableNumber, bestFitTable.Capacity, partySize);
                return new List<ReservationTableInfo> { bestFitTable };
            }

            // Strategy 3: If no single table works within allocation limits, try combining smaller tables
            _logger.LogInformation("No single table fits within allocation limit. Trying table combinations.");

            // Sort smaller tables by capacity in descending order
            var smallerTables = trulyAvailableTables
                .Where(t => t.Capacity < partySize)
                .OrderByDescending(t => t.Capacity)
                .ToList();

            if (smallerTables.Any())
            {
                var selectedTables = new List<ReservationTableInfo>();
                var remainingPartySize = partySize;

                foreach (var table in smallerTables)
                {
                    if (remainingPartySize <= 0) break;

                    selectedTables.Add(table);
                    remainingPartySize -= table.Capacity;
                }

                if (remainingPartySize <= 0)
                {
                    _logger.LogInformation("Found combination of {Count} tables with total capacity {TotalCapacity} for party of {PartySize}",
                        selectedTables.Count, selectedTables.Sum(t => t.Capacity), partySize);
                    return selectedTables;
                }
            }

            // If we couldn't find suitable tables
            _logger.LogWarning("Unable to find suitable table(s) for party of {PartySize} within allocation limit of {Limit}",
                partySize, remainingReservationCapacity);
            return new List<ReservationTableInfo>();
        }
        private (TimeSpan openingTime, TimeSpan closingTime, bool isOvernight) ParseOperatingHours(string operatingHoursString)
        {
            // Default values in case parsing fails
            TimeSpan defaultOpeningTime = new TimeSpan(11, 0, 0); // 11:00 AM
            TimeSpan defaultClosingTime = new TimeSpan(22, 0, 0); // 10:00 PM
            bool isOvernight = false;

            if (string.IsNullOrEmpty(operatingHoursString))
            {
                _logger.LogWarning("Operating hours string is null or empty, using default values");
                return (defaultOpeningTime, defaultClosingTime, isOvernight);
            }

            try
            {
                // Expected format: "10:00 AM - 10:00 PM"
                var parts = operatingHoursString.Split('-');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid operating hours format: {OperatingHours}, using default values", operatingHoursString);
                    return (defaultOpeningTime, defaultClosingTime, isOvernight);
                }

                string openingTimeStr = parts[0].Trim();
                string closingTimeStr = parts[1].Trim();

                // Parse the time strings to DateTime objects
                DateTime openingDateTime;
                DateTime closingDateTime;

                if (DateTime.TryParse(openingTimeStr, out openingDateTime) &&
                    DateTime.TryParse(closingTimeStr, out closingDateTime))
                {
                    // Extract TimeSpan values
                    TimeSpan opening = openingDateTime.TimeOfDay;
                    TimeSpan closing = closingDateTime.TimeOfDay;

                    // Check if it's an overnight operation (closing time is smaller than opening time)
                    if (closing < opening)
                    {
                        isOvernight = true;
                        _logger.LogInformation("Detected overnight hours: {OpeningTime} to {ClosingTime}",
                            FormatTimeSpan(opening), FormatTimeSpan(closing));
                    }

                    return (opening, closing, isOvernight);
                }
                else
                {
                    _logger.LogWarning("Failed to parse operating hours: {OperatingHours}, using default values", operatingHoursString);
                    return (defaultOpeningTime, defaultClosingTime, isOvernight);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing operating hours: {OperatingHours}, using default values", operatingHoursString);
                return (defaultOpeningTime, defaultClosingTime, isOvernight);
            }
        }

        private bool IsWithinOperatingHours(TimeSpan timeToCheck, TimeSpan opening, TimeSpan closing, bool isOvernight)
        {
            if (isOvernight)
            {
                // If overnight, either the time is after opening or before closing
                return timeToCheck >= opening || timeToCheck <= closing;
            }
            else
            {
                // Standard case: time must be between opening and closing
                return timeToCheck >= opening && timeToCheck <= closing;
            }
        }

        private string FormatTimeSpan(TimeSpan time)
        {
            int hour = time.Hours;
            string amPm = hour < 12 ? "AM" : "PM";

            // Convert to 12-hour format
            if (hour > 12)
                hour -= 12;
            else if (hour == 0)
                hour = 12;

            return $"{hour}:{time.Minutes:D2} {amPm}";
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

            // Filter for reservation tables first
            var reservationTablesInfo = new List<ReservationTableInfo>();
            foreach (var table in availableTables)
            {
                bool isReservationTable = await _tableTypeService.IsReservationTableAsync(outletId, table.Id, startTime);
                if (isReservationTable)
                {
                    reservationTablesInfo.Add(table);
                }
            }

            // If no reservation tables are available, log warning and continue with standard logic
            // This is a fallback in case the system is misconfigured or all tables are assigned to queue
            if (!reservationTablesInfo.Any())
            {
                _logger.LogWarning("No reservation tables found for outlet {OutletId} at {StartTime}. Using all available tables.",
                    outletId, startTime);
                reservationTablesInfo = availableTables;
            }

            // Get tables that are already reserved for this time slot
            var reservedTableIds = await _reservationRepository.GetReservedTableIdsForTimeSlotAsync(
                outletId, startTime, endTime);

            // Get reservation settings to determine limits
            var settings = await _outletAdapter.GetReservationSettingsAsync(outletId, startTime);
            if (settings == null)
            {
                _logger.LogWarning("Could not get reservation settings for outlet {OutletId}", outletId);
                return new List<ReservationTableInfo>();
            }

            // Filter out tables that are already reserved
            var trulyAvailableTables = reservationTablesInfo
                .Where(t => t.IsActive && !reservedTableIds.Contains(t.Id))
                .ToList();

            // Get current reservation capacity usage
            int reservedCapacity = await _reservationRepository.GetReservedCapacityForTimeSlotAsync(
                outletId, startTime, endTime);

            // Calculate remaining capacity for reservations
            int remainingReservationCapacity = settings.ReservationCapacity - reservedCapacity;

            // If the party size exceeds the remaining reservation capacity, fail
            if (partySize > remainingReservationCapacity)
            {
                _logger.LogWarning("Party size {PartySize} exceeds remaining reservation capacity {RemainingCapacity}",
                    partySize, remainingReservationCapacity);
                return new List<ReservationTableInfo>();
            }

            _logger.LogInformation("Searching for table(s) for party size {PartySize}, remaining capacity {RemainingCapacity}",
                partySize, remainingReservationCapacity);

            // CRITICAL FIX: The key issue is that we need to account for table capacity waste
            // If there's a 50% allocation limit, a 3-person party shouldn't get an 8-person table
            // as that would effectively use 8 seats from the allocated capacity

            // Strategy 1: Look for exact match first (best case - no waste)
            var exactMatchTable = trulyAvailableTables
                .FirstOrDefault(t => t.Capacity == partySize);

            if (exactMatchTable != null)
            {
                _logger.LogInformation("Found exact match table {TableNumber} with capacity {Capacity} for party size {PartySize}",
                    exactMatchTable.TableNumber, exactMatchTable.Capacity, partySize);
                return new List<ReservationTableInfo> { exactMatchTable };
            }

            // Strategy 2: Find a table that can accommodate the party WITHOUT exceeding allocation
            // We need to validate that the table's capacity doesn't push us over the allocation limit
            // If we have 7 seats total allocation and 3 are used, we can't use a table with 8 capacity for 3 more people
            // because that would waste 5 seats and push total used to 8 (exceeding our 7 limit)

            // Calculate the maximum table capacity we can use without exceeding the allocation
            int maxAllowableCapacity = remainingReservationCapacity;

            // Find tables that can accommodate the party and don't exceed the allocation
            var suitableTables = trulyAvailableTables
                .Where(t => t.Capacity >= partySize && t.Capacity <= maxAllowableCapacity)
                .OrderBy(t => t.Capacity)  // Get the smallest suitable table
                .ToList();

            if (suitableTables.Any())
            {
                var bestFitTable = suitableTables.First();
                _logger.LogInformation("Found suitable table {TableNumber} with capacity {Capacity} for party size {PartySize}",
                    bestFitTable.TableNumber, bestFitTable.Capacity, partySize);
                return new List<ReservationTableInfo> { bestFitTable };
            }

            // Strategy 3: If no single table works within allocation limits, try combining smaller tables
            _logger.LogInformation("No single table fits within allocation limit. Trying table combinations.");

            // Sort smaller tables by capacity in descending order
            var smallerTables = trulyAvailableTables
                .Where(t => t.Capacity < partySize)
                .OrderByDescending(t => t.Capacity)
                .ToList();

            if (smallerTables.Any())
            {
                var selectedTables = new List<ReservationTableInfo>();
                var remainingPartySize = partySize;

                foreach (var table in smallerTables)
                {
                    if (remainingPartySize <= 0) break;

                    selectedTables.Add(table);
                    remainingPartySize -= table.Capacity;
                }

                if (remainingPartySize <= 0)
                {
                    _logger.LogInformation("Found combination of {Count} tables with total capacity {TotalCapacity} for party of {PartySize}",
                        selectedTables.Count, selectedTables.Sum(t => t.Capacity), partySize);
                    return selectedTables;
                }
            }

            // If we couldn't find suitable tables
            _logger.LogWarning("Unable to find suitable table(s) for party of {PartySize} within allocation limit of {Limit}",
                partySize, remainingReservationCapacity);
            return new List<ReservationTableInfo>();
        }

        private async Task ScheduleRemindersAsync(Guid reservationId, DateTime reservationDate)
        {
            try
            {
                // Get ALL reminders for this reservation using repository
                var existingReminders = await _reservationRepository.GetAllRemindersByReservationIdAsync(reservationId);

                // For 24-hour reminder
                DateTime reminder24Hour = reservationDate.AddHours(-24);
                var existing24Hour = existingReminders.FirstOrDefault(r => r.ReminderType == "24Hour");

                if (existing24Hour != null)
                {
                    // Update existing reminder if it hasn't been sent yet
                    if (existing24Hour.Status == "Pending")
                    {
                        existing24Hour.ScheduledFor = reminder24Hour;
                        await _reservationRepository.UpdateReminderAsync(existing24Hour);
                        _logger.LogInformation("Updated existing 24-hour reminder for reservation: {ReservationId}", reservationId);
                    }
                }
                else if (reminder24Hour > DateTime.UtcNow)
                {
                    // Create new 24-hour reminder
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
                    _logger.LogInformation("Created new 24-hour reminder for reservation: {ReservationId}", reservationId);
                }

                // Handle 1-hour reminder similarly
                DateTime reminder1Hour = reservationDate.AddHours(-1);
                var existing1Hour = existingReminders.FirstOrDefault(r => r.ReminderType == "1Hour");

                if (existing1Hour != null)
                {
                    // Update existing reminder if it hasn't been sent yet
                    if (existing1Hour.Status == "Pending")
                    {
                        existing1Hour.ScheduledFor = reminder1Hour;
                        await _reservationRepository.UpdateReminderAsync(existing1Hour);
                        _logger.LogInformation("Updated existing 1-hour reminder for reservation: {ReservationId}", reservationId);
                    }
                }
                else if (reminder1Hour > DateTime.UtcNow)
                {
                    // Create new 1-hour reminder
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
                    _logger.LogInformation("Created new 1-hour reminder for reservation: {ReservationId}", reservationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling reminders for reservation: {ReservationId}", reservationId);
                // We'll handle this as a non-fatal error - the reservation is still updated
            }
        }

        private async Task ScheduleInitialConfirmationReminderAsync(Guid reservationId)
        {
            try
            {
                // Create confirmation reminder
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling confirmation reminder for reservation: {ReservationId}", reservationId);
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

        private async Task<List<ReservationTableInfo>> FindOptimalTableCombinationFromAvailableTablesAsync(
    List<ReservationTableInfo> availableTables,
    int partySize)
        {
            _logger.LogInformation("Finding optimal table combination for party size: {PartySize} from {Count} available tables",
                partySize, availableTables.Count);

            // Strategy 1: Look for exact match first (best case - no waste)
            var exactMatchTable = availableTables.FirstOrDefault(t => t.Capacity == partySize);

            if (exactMatchTable != null)
            {
                _logger.LogInformation("Found exact match table {TableNumber} with capacity {Capacity} for party size {PartySize}",
                    exactMatchTable.TableNumber, exactMatchTable.Capacity, partySize);
                return new List<ReservationTableInfo> { exactMatchTable };
            }

            // Strategy 2: Find a table that can accommodate the party
            var suitableTables = availableTables
                .Where(t => t.Capacity >= partySize)
                .OrderBy(t => t.Capacity)  // Get the smallest suitable table (minimize waste)
                .ToList();

            if (suitableTables.Any())
            {
                var bestFitTable = suitableTables.First();
                _logger.LogInformation("Found suitable table {TableNumber} with capacity {Capacity} for party size {PartySize}",
                    bestFitTable.TableNumber, bestFitTable.Capacity, partySize);
                return new List<ReservationTableInfo> { bestFitTable };
            }

            // Strategy 3: Try combining smaller tables
            var smallerTables = availableTables
                .Where(t => t.Capacity < partySize)
                .OrderByDescending(t => t.Capacity)  // Start with the largest of the smaller tables
                .ToList();

            if (smallerTables.Any())
            {
                var selectedTables = new List<ReservationTableInfo>();
                var remainingPartySize = partySize;

                foreach (var table in smallerTables)
                {
                    if (remainingPartySize <= 0) break;

                    selectedTables.Add(table);
                    remainingPartySize -= table.Capacity;
                }

                if (remainingPartySize <= 0)
                {
                    _logger.LogInformation("Found combination of {Count} tables with total capacity {TotalCapacity} for party of {PartySize}",
                        selectedTables.Count, selectedTables.Sum(t => t.Capacity), partySize);
                    return selectedTables;
                }
            }

            // If we couldn't find suitable tables
            _logger.LogWarning("Unable to find suitable table(s) for party of {PartySize}", partySize);
            return new List<ReservationTableInfo>();
        }

        // Update the method signature
        private bool CanAccommodatePartySize(List<FNBReservation.Modules.Outlet.Core.DTOs.TableDto> availableTables, int partySize)
        {
            // Strategy 1: Check for exact match first
            if (availableTables.Any(t => t.Capacity == partySize))
                return true;

            // Strategy 2: Check if any single table can accommodate the party
            if (availableTables.Any(t => t.Capacity >= partySize))
                return true;

            // Strategy 3: Check if a combination of tables can accommodate the party
            var smallerTables = availableTables
                .Where(t => t.Capacity < partySize)
                .OrderByDescending(t => t.Capacity)
                .ToList();

            if (smallerTables.Any())
            {
                int combinedCapacity = 0;
                foreach (var table in smallerTables)
                {
                    combinedCapacity += table.Capacity;
                    if (combinedCapacity >= partySize)
                        return true;
                }
            }

            return false;
        }
    }
}
        #endregion