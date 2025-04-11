using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.DTOs;
using FNBReservation.Modules.Queue.Core.Entities;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using ReservationTableInfo = FNBReservation.Modules.Reservation.Core.Interfaces.TableInfo;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Queue.Infrastructure.Services
{
    public class QueueService : IQueueService
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueNotificationService _notificationService;
        private readonly IWaitTimeEstimationService _waitTimeEstimationService;
        private readonly IOutletService _outletService;
        private readonly ITableService _tableService;
        private readonly ITableTypeService _tableTypeService; // Add this
        private readonly IQueueHub _queueHub;
        private readonly ILogger<QueueService> _logger;

        public QueueService(
            IQueueRepository queueRepository,
            IQueueNotificationService notificationService,
            IWaitTimeEstimationService waitTimeEstimationService,
            IOutletService outletService,
            ITableService tableService,
            ITableTypeService tableTypeService, // Add this
            IQueueHub queueHub,
            ILogger<QueueService> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _waitTimeEstimationService = waitTimeEstimationService ?? throw new ArgumentNullException(nameof(waitTimeEstimationService));
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
            _tableTypeService = tableTypeService ?? throw new ArgumentNullException(nameof(tableTypeService)); // Add this
            _queueHub = queueHub ?? throw new ArgumentNullException(nameof(queueHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<QueueEntryDto> CreateQueueEntryAsync(CreateQueueEntryDto createQueueEntryDto)
        {
            _logger.LogInformation("Creating queue entry for {CustomerName} at outlet {OutletId}",
                createQueueEntryDto.CustomerName, createQueueEntryDto.OutletId);

            try
            {
                // Validate outlet exists
                var outlet = await _outletService.GetOutletByIdAsync(createQueueEntryDto.OutletId);
                if (outlet == null)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", createQueueEntryDto.OutletId);
                    throw new ArgumentException($"Outlet with ID {createQueueEntryDto.OutletId} not found");
                }

                // Check if queue is enabled for this outlet
                if (!outlet.QueueEnabled)
                {
                    _logger.LogWarning("Queue is disabled for outlet: {OutletId}", createQueueEntryDto.OutletId);
                    throw new InvalidOperationException("Queue is currently disabled for this outlet");
                }

                // Generate a unique queue code
                string queueCode = await GenerateQueueCodeAsync(createQueueEntryDto.OutletId);

                // Create queue entry
                var queueEntry = new QueueEntry
                {
                    Id = Guid.NewGuid(),
                    QueueCode = queueCode,
                    OutletId = createQueueEntryDto.OutletId,
                    CustomerName = createQueueEntryDto.CustomerName,
                    CustomerPhone = createQueueEntryDto.CustomerPhone,
                    PartySize = createQueueEntryDto.PartySize,
                    SpecialRequests = createQueueEntryDto.SpecialRequests,
                    Status = "Waiting",
                    QueuePosition = 0, // Will be set by repository
                    QueuedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsHeld = false,
                    EstimatedWaitMinutes = 0 // Will be calculated
                };

                // Save queue entry
                var createdEntry = await _queueRepository.CreateAsync(queueEntry);

                // Add initial status change
                var statusChange = new QueueStatusChange
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = createdEntry.Id,
                    OldStatus = "",
                    NewStatus = "Waiting",
                    ChangedAt = DateTime.UtcNow,
                    Reason = "Initial queue entry"
                };

                await _queueRepository.AddStatusChangeAsync(statusChange);

                // Calculate estimated wait time
                var estimatedWait = await _waitTimeEstimationService.EstimateWaitTimeAsync(
                    createdEntry.OutletId,
                    createdEntry.PartySize,
                    createdEntry.QueuePosition);

                createdEntry.EstimatedWaitMinutes = estimatedWait;
                await _queueRepository.UpdateAsync(createdEntry);

                // Send confirmation
                await _notificationService.SendQueueConfirmationAsync(createdEntry.Id);

                // Notify clients about queue update
                await _queueHub.NotifyQueueUpdated(createdEntry.OutletId);

                // Map to DTO and return
                return await MapToQueueEntryDtoAsync(createdEntry, outlet.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating queue entry");
                throw;
            }
        }

        public async Task<QueueEntryDto> GetQueueEntryByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting queue entry by ID: {QueueEntryId}", id);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(id);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", id);
                    return null;
                }

                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                return await MapToQueueEntryDtoAsync(queueEntry, outlet?.Name ?? "Unknown Outlet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entry: {QueueEntryId}", id);
                throw;
            }
        }

        public async Task<QueueEntryDto> GetQueueEntryByCodeAsync(string queueCode)
        {
            _logger.LogInformation("Getting queue entry by code: {QueueCode}", queueCode);

            try
            {
                var queueEntry = await _queueRepository.GetByCodeAsync(queueCode);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueCode}", queueCode);
                    return null;
                }

                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                return await MapToQueueEntryDtoAsync(queueEntry, outlet?.Name ?? "Unknown Outlet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entry by code: {QueueCode}", queueCode);
                throw;
            }
        }

        public async Task<IEnumerable<QueueEntryDto>> GetQueueEntriesByOutletIdAsync(Guid outletId, string status = null)
        {
            _logger.LogInformation("Getting queue entries for outlet: {OutletId}, Status: {Status}",
                outletId, status);

            try
            {
                var queueEntries = await _queueRepository.GetByOutletIdAsync(outletId, status);
                var outlet = await _outletService.GetOutletByIdAsync(outletId);
                string outletName = outlet?.Name ?? "Unknown Outlet";

                var entryDtos = new List<QueueEntryDto>();
                foreach (var entry in queueEntries)
                {
                    entryDtos.Add(await MapToQueueEntryDtoAsync(entry, outletName));
                }

                return entryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entries for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<QueueEntryDto> UpdateQueueEntryAsync(Guid id, UpdateQueueEntryDto updateQueueEntryDto)
        {
            _logger.LogInformation("Updating queue entry: {QueueEntryId}", id);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(id);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", id);
                    return null;
                }

                // Only allow updates to entries in Waiting status
                if (queueEntry.Status != "Waiting")
                {
                    _logger.LogWarning("Cannot update queue entry with status {Status}: {QueueEntryId}",
                        queueEntry.Status, id);
                    throw new InvalidOperationException($"Cannot update queue entry with status '{queueEntry.Status}'");
                }

                // Update properties if provided
                if (!string.IsNullOrEmpty(updateQueueEntryDto.CustomerName))
                {
                    queueEntry.CustomerName = updateQueueEntryDto.CustomerName;
                }

                if (!string.IsNullOrEmpty(updateQueueEntryDto.CustomerPhone))
                {
                    queueEntry.CustomerPhone = updateQueueEntryDto.CustomerPhone;
                }

                if (updateQueueEntryDto.PartySize.HasValue)
                {
                    // Party size change might affect wait time estimation
                    queueEntry.PartySize = updateQueueEntryDto.PartySize.Value;
                    queueEntry.EstimatedWaitMinutes = await _waitTimeEstimationService.EstimateWaitTimeAsync(
                        queueEntry.OutletId, queueEntry.PartySize, queueEntry.QueuePosition);
                }

                if (updateQueueEntryDto.SpecialRequests != null) // Allow empty string
                {
                    queueEntry.SpecialRequests = updateQueueEntryDto.SpecialRequests;
                }

                queueEntry.UpdatedAt = DateTime.UtcNow;
                var updatedEntry = await _queueRepository.UpdateAsync(queueEntry);

                // Notify clients about queue update
                await _queueHub.NotifyQueueUpdated(updatedEntry.OutletId);

                // Update specific customer's view
                await NotifyCustomerOfQueueStatusUpdateAsync(updatedEntry);

                var outlet = await _outletService.GetOutletByIdAsync(updatedEntry.OutletId);
                return await MapToQueueEntryDtoAsync(updatedEntry, outlet?.Name ?? "Unknown Outlet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue entry: {QueueEntryId}", id);
                throw;
            }
        }

        public async Task<QueueEntryDto> UpdateQueueStatusAsync(Guid id, QueueStatusUpdateDto statusUpdateDto, Guid? staffId = null)
        {
            _logger.LogInformation("Updating queue entry status: {QueueEntryId} to {Status}",
                id, statusUpdateDto.Status);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(id);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", id);
                    return null;
                }

                string oldStatus = queueEntry.Status;

                // Validate status transition
                ValidateStatusTransition(oldStatus, statusUpdateDto.Status);

                // Update status and timestamps
                queueEntry.Status = statusUpdateDto.Status;
                queueEntry.UpdatedAt = DateTime.UtcNow;

                // Handle specific status updates
                switch (statusUpdateDto.Status)
                {
                    case "Called":
                        queueEntry.CalledAt = DateTime.UtcNow;
                        queueEntry.QueuePosition = 0;
                        // Remove this call to ReorderQueueAsync - we'll do it once at the end
                        // await ReorderQueueAsync(queueEntry.OutletId);
                        break;
                    case "Seated":
                        queueEntry.SeatedAt = DateTime.UtcNow;
                        break;
                    case "Completed":
                        queueEntry.CompletedAt = DateTime.UtcNow;
                        break;
                }

                // Add status change record
                var statusChange = new QueueStatusChange
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntry.Id,
                    OldStatus = oldStatus,
                    NewStatus = statusUpdateDto.Status,
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = staffId,
                    Reason = statusUpdateDto.Reason
                };

                await _queueRepository.AddStatusChangeAsync(statusChange);
                var updatedEntry = await _queueRepository.UpdateAsync(queueEntry);

                // Update table assignments if needed
                if (statusUpdateDto.Status == "Seated")
                {
                    foreach (var assignment in queueEntry.TableAssignments)
                    {
                        assignment.Status = "Seated";
                        assignment.SeatedAt = DateTime.UtcNow;
                        await _queueRepository.UpdateTableAssignmentAsync(assignment);
                    }
                }
                else if (statusUpdateDto.Status == "Completed" || statusUpdateDto.Status == "NoShow" || statusUpdateDto.Status == "Cancelled")
                {
                    foreach (var assignment in queueEntry.TableAssignments)
                    {
                        assignment.Status = statusUpdateDto.Status;
                        if (statusUpdateDto.Status == "Completed")
                            assignment.CompletedAt = DateTime.UtcNow;
                        await _queueRepository.UpdateTableAssignmentAsync(assignment);
                    }
                }

                // Send table ready notification if status changed to Called
                if (statusUpdateDto.Status == "Called")
                {
                    await _notificationService.SendTableReadyNotificationAsync(updatedEntry.Id);
                }

                // Single consolidated check to determine if we need to reorder and update wait times
                bool shouldReorderAndUpdate = statusUpdateDto.Status == "Called" ||
                                             statusUpdateDto.Status == "Seated" ||
                                             statusUpdateDto.Status == "Completed" ||
                                             statusUpdateDto.Status == "NoShow" ||
                                             statusUpdateDto.Status == "Cancelled";

                if (shouldReorderAndUpdate)
                {
                    // This single call will handle both reordering and notifications
                    await ReorderQueueAsync(updatedEntry.OutletId);

                    // Update wait times for all remaining customers
                    await UpdateWaitTimesAsync(updatedEntry.OutletId);
                }
                else
                {
                    // Only notify about this specific customer's status change
                    await NotifyCustomerOfQueueStatusUpdateAsync(updatedEntry);
                }

                // Remove this direct call - we're centralizing notifications through ReorderQueueAsync
                // await _queueHub.NotifyQueueUpdated(updatedEntry.OutletId);

                var outlet = await _outletService.GetOutletByIdAsync(updatedEntry.OutletId);
                return await MapToQueueEntryDtoAsync(updatedEntry, outlet?.Name ?? "Unknown Outlet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue entry status: {QueueEntryId}", id);
                throw;
            }
        }

        public async Task<bool> CancelQueueEntryAsync(Guid id, string reason, Guid? staffId = null)
        {
            _logger.LogInformation("Cancelling queue entry: {QueueEntryId}", id);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(id);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", id);
                    return false;
                }

                if (queueEntry.Status != "Waiting" && queueEntry.Status != "Called")
                {
                    _logger.LogWarning("Cannot cancel queue entry with status {Status}: {QueueEntryId}",
                        queueEntry.Status, id);
                    return false;
                }

                // Update table assignments
                foreach (var assignment in queueEntry.TableAssignments)
                {
                    assignment.Status = "Cancelled";
                    await _queueRepository.UpdateTableAssignmentAsync(assignment);
                }

                string oldStatus = queueEntry.Status;
                queueEntry.Status = "Cancelled";
                queueEntry.UpdatedAt = DateTime.UtcNow;

                queueEntry.QueuePosition = 0;

                // Add status change record
                var statusChange = new QueueStatusChange
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntry.Id,
                    OldStatus = oldStatus,
                    NewStatus = "Cancelled",
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = staffId,
                    Reason = reason
                };

                await _queueRepository.AddStatusChangeAsync(statusChange);
                await _queueRepository.UpdateAsync(queueEntry);

                // Send cancellation notification
                await _notificationService.SendQueueCancellationAsync(queueEntry.Id, reason);

                // Reorder queue since an entry was removed
                await ReorderQueueAsync(queueEntry.OutletId);

                // Notify clients about queue update
                await _queueHub.NotifyQueueUpdated(queueEntry.OutletId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling queue entry: {QueueEntryId}", id);
                throw;
            }
        }

        public async Task<QueueEntryDto> AssignTableToQueueEntryAsync(Guid queueEntryId, AssignTableDto assignTableDto)
        {
            _logger.LogInformation("Assigning table {TableId} to queue entry: {QueueEntryId}",
                assignTableDto.TableId, queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    return null;
                }

                if (queueEntry.Status != "Waiting" && queueEntry.Status != "Called")
                {
                    _logger.LogWarning("Cannot assign table to queue entry with status {Status}: {QueueEntryId}",
                        queueEntry.Status, queueEntryId);
                    throw new InvalidOperationException($"Cannot assign table to queue entry with status '{queueEntry.Status}'");
                }

                // Get table details
                var table = await _tableService.GetTableByIdAsync(assignTableDto.TableId);
                if (table == null)
                {
                    _logger.LogWarning("Table not found: {TableId}", assignTableDto.TableId);
                    throw new ArgumentException($"Table with ID {assignTableDto.TableId} not found");
                }

                // CRITICAL: Validate table capacity against party size
                if (table.Capacity < queueEntry.PartySize)
                {
                    // If staff has confirmed the overflow, allow it but log a warning
                    if (assignTableDto.StaffConfirmedOverflow)
                    {
                        _logger.LogWarning("Table {TableId} with capacity {Capacity} is being assigned to party size {PartySize} with staff approval",
                            assignTableDto.TableId, table.Capacity, queueEntry.PartySize);

                        // Add a note to the assignment
                        queueEntry.SpecialRequests = (queueEntry.SpecialRequests ?? "") +
                            $"\n[SYSTEM] Table capacity ({table.Capacity}) less than party size ({queueEntry.PartySize}). Staff confirmed assignment.";
                    }
                    else
                    {
                        // If staff hasn't confirmed, require confirmation
                        _logger.LogWarning("Table {TableId} with capacity {Capacity} is too small for party size {PartySize}",
                            assignTableDto.TableId, table.Capacity, queueEntry.PartySize);
                        throw new InvalidOperationException($"Table {table.TableNumber} with capacity {table.Capacity} is too small for party of {queueEntry.PartySize}. " +
                            $"Staff confirmation required for this assignment.");
                    }
                }

                // Check if this is a queue table
                bool isQueueTable = await _tableTypeService.IsQueueTableAsync(
                    queueEntry.OutletId, assignTableDto.TableId, DateTime.UtcNow);

                if (!isQueueTable)
                {
                    _logger.LogWarning("Table {TableId} is not a queue table for outlet {OutletId}",
                        assignTableDto.TableId, queueEntry.OutletId);
                    throw new InvalidOperationException($"Table {table.TableNumber} is reserved for reservations and cannot be assigned to queue customers");
                }

                // Check if table is already assigned to an active queue entry
                var existingAssignments = await _queueRepository.GetActiveTableAssignmentsAsync(assignTableDto.TableId);
                if (existingAssignments.Any())
                {
                    _logger.LogWarning("Table {TableId} is already assigned to an active queue entry",
                        assignTableDto.TableId);
                    throw new InvalidOperationException($"Table {table.TableNumber} is already assigned to another customer");
                }

                // Create table assignment
                var tableAssignment = new QueueTableAssignment
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntryId,
                    TableId = assignTableDto.TableId,
                    TableNumber = table.TableNumber,
                    Status = "Assigned",
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = assignTableDto.StaffId
                };

                await _queueRepository.AddTableAssignmentAsync(tableAssignment);

                // Update status to Called if currently Waiting
                if (queueEntry.Status == "Waiting")
                {
                    var statusUpdateDto = new QueueStatusUpdateDto
                    {
                        Status = "Called",
                        Reason = "Table assigned by staff"
                    };

                    return await UpdateQueueStatusAsync(queueEntryId, statusUpdateDto, assignTableDto.StaffId);
                }

                // Get outlet
                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);

                // Notify the customer that their table is ready
                await _queueHub.NotifyTableReady(queueEntryId, table.TableNumber);

                return await MapToQueueEntryDtoAsync(queueEntry, outlet?.Name ?? "Unknown Outlet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning table to queue entry: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task<QueueEntryDto> MarkQueueEntryAsSeatedAsync(Guid queueEntryId, Guid staffId)
        {
            _logger.LogInformation("Marking queue entry as seated: {QueueEntryId}", queueEntryId);

            try
            {

                // First, get the queue entry
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    throw new ArgumentException($"Queue entry with ID {queueEntryId} not found");
                }

                foreach (var assignment in queueEntry.TableAssignments)
                {
                    assignment.Status = "Seated";
                    assignment.SeatedAt = DateTime.UtcNow;
                    await _queueRepository.UpdateTableAssignmentAsync(assignment);
                }

                var statusUpdateDto = new QueueStatusUpdateDto
                {
                    Status = "Seated",
                    Reason = "Marked as seated by staff"
                };

                return await UpdateQueueStatusAsync(queueEntryId, statusUpdateDto, staffId);

            }   
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking queue entry as seated: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task<QueueEntryDto> MarkQueueEntryAsCompletedAsync(Guid queueEntryId, Guid staffId)
        {
            _logger.LogInformation("Marking queue entry as completed: {QueueEntryId}", queueEntryId);

            try
            {
                // First, get the queue entry
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    throw new ArgumentException($"Queue entry with ID {queueEntryId} not found");
                }

                foreach (var assignment in queueEntry.TableAssignments)
                {
                    assignment.Status = "Completed";
                    assignment.CompletedAt = DateTime.UtcNow;
                    await _queueRepository.UpdateTableAssignmentAsync(assignment);
                }

                var statusUpdateDto = new QueueStatusUpdateDto
                {
                    Status = "Completed",
                    Reason = "Marked as completed by staff"
                };

                var updatedEntry = await UpdateQueueStatusAsync(queueEntryId, statusUpdateDto, staffId);

                // Get the tables that were assigned to this queue entry
                var freedTables = updatedEntry.TableAssignments.Select(ta => ta.TableId).ToList();

                // Check if there are any waiting customers for these newly available tables
                foreach (var tableId in freedTables)
                {
                    var recommendation = await GetTableRecommendationAsync(updatedEntry.OutletId, tableId);
                }

                return updatedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking queue entry as completed: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task<QueueEntryDto> MarkQueueEntryAsNoShowAsync(Guid queueEntryId, Guid staffId)
        {
            _logger.LogInformation("Marking queue entry as no-show: {QueueEntryId}", queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    throw new ArgumentException($"Queue entry with ID {queueEntryId} not found");
                }

                foreach (var assignment in queueEntry.TableAssignments)
                {
                    assignment.Status = "NoShow";
                    await _queueRepository.UpdateTableAssignmentAsync(assignment);
                }

                var statusUpdateDto = new QueueStatusUpdateDto
                {
                    Status = "NoShow",
                    Reason = "Marked as no-show by staff"
                };

                return await UpdateQueueStatusAsync(queueEntryId, statusUpdateDto, staffId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking queue entry as no-show: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task<QueueEntryListResponseDto> GetQueueEntriesAsync(
            Guid outletId,
            List<string> statuses = null,
            string searchTerm = null,
            int page = 1,
            int pageSize = 20)
        {
            _logger.LogInformation("Getting queue entries for outlet: {OutletId}, page: {Page}", outletId, page);

            try
            {
                var result = await _queueRepository.SearchEntriesAsync(
                    outletId, statuses, searchTerm, page, pageSize);

                var entries = result.Entries;
                var totalCount = result.TotalCount;

                // Get outlet information for outletName
                var outlet = await _outletService.GetOutletByIdAsync(outletId);
                string outletName = outlet?.Name ?? "Unknown Outlet";

                // Map entries to DTOs
                var entryDtos = new List<QueueEntryDto>();
                foreach (var entry in entries)
                {
                    entryDtos.Add(await MapToQueueEntryDtoAsync(entry, outletName));
                }

                // Prepare response
                return new QueueEntryListResponseDto
                {
                    Entries = entryDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entries for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<QueueSummaryDto> GetQueueSummaryAsync(Guid outletId)
        {
            _logger.LogInformation("Getting queue summary for outlet: {OutletId}", outletId);

            try
            {
                int totalWaiting = await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Waiting");
                int totalCalled = await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Called");
                int totalSeated = await _queueRepository.CountQueueEntriesByStatusAsync(outletId, "Seated");
                int longestWait = await _queueRepository.GetLongestWaitTimeAsync(outletId);
                int averageWait = await _queueRepository.GetAverageWaitTimeAsync(outletId);

                return new QueueSummaryDto
                {
                    TotalWaiting = totalWaiting,
                    TotalCalled = totalCalled,
                    TotalSeated = totalSeated,
                    AverageWaitMinutes = averageWait,
                    LongestWaitMinutes = longestWait
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue summary for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<int> GetEstimatedWaitTimeAsync(Guid outletId, int partySize)
        {
            _logger.LogInformation("Getting estimated wait time for party size {PartySize} at outlet: {OutletId}",
                partySize, outletId);

            try
            {
                // Count current entries in queue to determine position
                int queuePosition = await _queueRepository.CountActiveQueueEntriesAsync(outletId) + 1;

                // Get estimate from service
                return await _waitTimeEstimationService.EstimateWaitTimeAsync(outletId, partySize, queuePosition);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting estimated wait time for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<QueueEntryDto> CallNextCustomerAsync(Guid outletId, Guid tableId, Guid staffId)
        {
            _logger.LogInformation("Calling next customer for table {TableId} at outlet: {OutletId}",
                tableId, outletId);

            try
            {
                // Get table information
                var table = await _tableService.GetTableByIdAsync(tableId);
                if (table == null)
                {
                    _logger.LogWarning("Table not found: {TableId}", tableId);
                    throw new ArgumentException($"Table with ID {tableId} not found");
                }

                // Get recommendation for this table
                var recommendation = await GetTableRecommendationAsync(outletId, tableId);

                // Get all waiting entries to properly track which ones are skipped
                var activeEntries = await _queueRepository.GetByOutletIdAsync(outletId, "Waiting");
                var nextCustomer = activeEntries
                    .OrderBy(e => e.IsHeld) // Non-held entries first
                    .ThenBy(e => e.QueuePosition)
                    .FirstOrDefault();

                // If we have a recommendation, use that customer
                if (recommendation != null)
                {
                    var queueEntry = await _queueRepository.GetByIdAsync(recommendation.QueueEntryId);
                    if (queueEntry != null && (queueEntry.Status == "Waiting" || queueEntry.Status == "Held"))
                    {
                        // Check if recommendation is not the next customer in line
                        if (nextCustomer != null && recommendation.QueueEntryId != nextCustomer.Id)
                        {
                            // Get all customers being skipped (those with lower queue position)
                            var skippedCustomers = activeEntries
                                .Where(e => e.QueuePosition < queueEntry.QueuePosition && !e.IsHeld)
                                .ToList();

                            foreach (var skipped in skippedCustomers)
                            {
                                // Mark as held
                                skipped.IsHeld = true;
                                skipped.HeldSince = DateTime.UtcNow;
                                await _queueRepository.UpdateAsync(skipped);

                                // Notify customer they're being held
                                await NotifyCustomerOfQueueStatusUpdateAsync(skipped);
                            }
                        }

                        // Assign the table
                        var assignTableDto = new AssignTableDto
                        {
                            QueueEntryId = queueEntry.Id,
                            TableId = tableId,
                            StaffId = staffId
                        };

                        // AssignTableToQueueEntryAsync will handle the necessary reordering internally
                        return await AssignTableToQueueEntryAsync(queueEntry.Id, assignTableDto);
                    }
                }

                // If no recommendation or recommended customer is no longer available,
                // get next customer in queue
                if (nextCustomer == null)
                {
                    _logger.LogWarning("No customers in queue for outlet: {OutletId}", outletId);
                    throw new InvalidOperationException("No customers in queue");
                }

                // Assign the table
                var assignTableRequest = new AssignTableDto
                {
                    QueueEntryId = nextCustomer.Id,
                    TableId = tableId,
                    StaffId = staffId
                };

                // Remove the explicit call to ReorderQueueAsync below, as it will be handled in AssignTableToQueueEntryAsync
                return await AssignTableToQueueEntryAsync(nextCustomer.Id, assignTableRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling next customer for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<TableRecommendationDto> GetTableRecommendationAsync(Guid outletId, Guid tableId)
        {
            _logger.LogInformation("Getting table recommendation for table {TableId} at outlet: {OutletId}",
                tableId, outletId);

            try
            {
                // Get table information
                var table = await _tableService.GetTableByIdAsync(tableId);
                if (table == null)
                {
                    _logger.LogWarning("Table not found: {TableId}", tableId);
                    throw new ArgumentException($"Table with ID {tableId} not found");
                }

                // Verify this is a queue table
                bool isQueueTable = await _tableTypeService.IsQueueTableAsync(outletId, tableId, DateTime.UtcNow);
                if (!isQueueTable)
                {
                    _logger.LogWarning("Table {TableId} is not a queue table", tableId);
                    throw new ArgumentException($"Table {table.TableNumber} is reserved for reservations and cannot be used for queue customers");
                }

                // Get active entries in the queue
                var activeEntries = await _queueRepository.GetByOutletIdAsync(outletId, "Waiting");
                if (!activeEntries.Any())
                {
                    _logger.LogInformation("No active entries in queue for outlet: {OutletId}", outletId);
                    return null;
                }

                // Order by position (non-held entries first)
                var orderedEntries = activeEntries
                    .OrderBy(e => e.IsHeld)
                    .ThenBy(e => e.QueuePosition)
                    .ToList();

                // Get the next customer in line
                var nextCustomer = orderedEntries.FirstOrDefault();
                if (nextCustomer == null)
                {
                    return null;
                }

                // Get all tables for this outlet
                var allTables = await _tableService.GetTablesByOutletIdAsync(outletId);

                // Filter for queue tables
                var queueTables = new List<TableDto>();
                foreach (var t in allTables)
                {
                    bool isTableForQueue = await _tableTypeService.IsQueueTableAsync(outletId, t.Id, DateTime.UtcNow);
                    if (isTableForQueue)
                    {
                        queueTables.Add(t);
                    }
                }

                // Find all customers that can fit at this table
                var eligibleCustomers = activeEntries
                    .Where(e => e.PartySize <= table.Capacity)
                    .ToList();

                if (!eligibleCustomers.Any())
                {
                    // No customers fit this table - recommend the next in line with a warning
                    return new TableRecommendationDto
                    {
                        QueueEntryId = nextCustomer.Id,
                        QueueCode = nextCustomer.QueueCode,
                        CustomerName = nextCustomer.CustomerName,
                        PartySize = nextCustomer.PartySize,
                        TableId = tableId,
                        TableNumber = table.TableNumber,
                        TableCapacity = table.Capacity,
                        RecommendationType = "TooSmall",
                        RecommendationMessage = $"Warning: Next group size ({nextCustomer.PartySize}) exceeds table capacity ({table.Capacity})"
                    };
                }

                // Find the optimal customer - prioritize:
                // 1. Best table utilization (closest party size to table capacity)
                // 2. Waiting time (queue position)
                // 3. Non-held status

                // First check if there's a perfect match (party size exactly matches table capacity)
                var perfectMatch = eligibleCustomers
                    .Where(e => e.PartySize == table.Capacity)
                    .OrderBy(e => e.IsHeld)
                    .ThenBy(e => e.QueuePosition)
                    .FirstOrDefault();

                if (perfectMatch != null)
                {
                    return new TableRecommendationDto
                    {
                        QueueEntryId = perfectMatch.Id,
                        QueueCode = perfectMatch.QueueCode,
                        CustomerName = perfectMatch.CustomerName,
                        PartySize = perfectMatch.PartySize,
                        TableId = tableId,
                        TableNumber = table.TableNumber,
                        TableCapacity = table.Capacity,
                        RecommendationType = "Optimal",
                        RecommendationMessage = $"Perfect match: {perfectMatch.CustomerName} (party of {perfectMatch.PartySize}) perfectly fits table capacity ({table.Capacity})"
                    };
                }

                // Next, find the customer with the largest party size that fits the table
                var bestFitCustomer = eligibleCustomers
                    .OrderByDescending(e => e.PartySize) // Prioritize larger parties
                    .ThenBy(e => e.IsHeld) // Then non-held entries
                    .ThenBy(e => e.QueuePosition) // Then respect queue position
                    .FirstOrDefault();

                // Check if the best fit is the next customer - if so, simple case
                if (bestFitCustomer?.Id == nextCustomer.Id)
                {
                    return new TableRecommendationDto
                    {
                        QueueEntryId = nextCustomer.Id,
                        QueueCode = nextCustomer.QueueCode,
                        CustomerName = nextCustomer.CustomerName,
                        PartySize = nextCustomer.PartySize,
                        TableId = tableId,
                        TableNumber = table.TableNumber,
                        TableCapacity = table.Capacity,
                        RecommendationType = "Optimal",
                        RecommendationMessage = $"Next customer: {nextCustomer.CustomerName} (party of {nextCustomer.PartySize})"
                    };
                }

                // If the best fit isn't next in line, we need to decide whether to optimize for
                // queue order or table utilization

                // Calculate the utilization percentage for the next customer and best fit
                double nextCustomerUtilization = nextCustomer.PartySize <= table.Capacity
                    ? (double)nextCustomer.PartySize / table.Capacity * 100
                    : 0;

                double bestFitUtilization = (double)bestFitCustomer.PartySize / table.Capacity * 100;

                // If the utilization difference is significant (>20 percentage points), recommend the best fit
                if (bestFitUtilization - nextCustomerUtilization >= 20)
                {
                    return new TableRecommendationDto
                    {
                        QueueEntryId = bestFitCustomer.Id,
                        QueueCode = bestFitCustomer.QueueCode,
                        CustomerName = bestFitCustomer.CustomerName,
                        PartySize = bestFitCustomer.PartySize,
                        TableId = tableId,
                        TableNumber = table.TableNumber,
                        TableCapacity = table.Capacity,
                        RecommendationType = "Optimal",
                        RecommendationMessage = $"Recommended: {bestFitCustomer.CustomerName} (party of {bestFitCustomer.PartySize}) is a better fit for table capacity ({table.Capacity})"
                    };
                }

                // Otherwise, respect the queue order if the next customer fits
                if (nextCustomer.PartySize <= table.Capacity)
                {
                    return new TableRecommendationDto
                    {
                        QueueEntryId = nextCustomer.Id,
                        QueueCode = nextCustomer.QueueCode,
                        CustomerName = nextCustomer.CustomerName,
                        PartySize = nextCustomer.PartySize,
                        TableId = tableId,
                        TableNumber = table.TableNumber,
                        TableCapacity = table.Capacity,
                        RecommendationType = "Optimal",
                        RecommendationMessage = $"Next customer: {nextCustomer.CustomerName} (party of {nextCustomer.PartySize})"
                    };
                }

                // If the next customer doesn't fit, recommend the best fit
                return new TableRecommendationDto
                {
                    QueueEntryId = bestFitCustomer.Id,
                    QueueCode = bestFitCustomer.QueueCode,
                    CustomerName = bestFitCustomer.CustomerName,
                    PartySize = bestFitCustomer.PartySize,
                    TableId = tableId,
                    TableNumber = table.TableNumber,
                    TableCapacity = table.Capacity,
                    RecommendationType = "Optimal",
                    RecommendationMessage = $"Recommended: {bestFitCustomer.CustomerName} (party of {bestFitCustomer.PartySize}) fits this table"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table recommendation for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<List<QueueEntryDto>> GetHeldEntriesAsync(Guid outletId)
        {
            _logger.LogInformation("Getting held entries for outlet: {OutletId}", outletId);

            try
            {
                var heldEntries = await _queueRepository.GetHeldEntriesAsync(outletId);
                var outlet = await _outletService.GetOutletByIdAsync(outletId);
                string outletName = outlet?.Name ?? "Unknown Outlet";

                var entryDtos = new List<QueueEntryDto>();
                foreach (var entry in heldEntries)
                {
                    entryDtos.Add(await MapToQueueEntryDtoAsync(entry, outletName));
                }

                return entryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting held entries for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task<QueueEntryDto> PrioritizeHeldEntryAsync(Guid queueEntryId, Guid staffId)
        {
            _logger.LogInformation("Prioritizing held entry: {QueueEntryId}", queueEntryId);

            try
            {
                var queueEntry = await _queueRepository.GetByIdAsync(queueEntryId);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found: {QueueEntryId}", queueEntryId);
                    return null;
                }

                if (!queueEntry.IsHeld)
                {
                    _logger.LogWarning("Queue entry is not held: {QueueEntryId}", queueEntryId);
                    throw new InvalidOperationException("Queue entry is not held");
                }

                // Un-hold the entry and move it to the front of the queue
                queueEntry.IsHeld = false;
                queueEntry.HeldSince = null;
                queueEntry.QueuePosition = 1; // Move to front of queue
                queueEntry.UpdatedAt = DateTime.UtcNow;

                // Add status change
                var statusChange = new QueueStatusChange
                {
                    Id = Guid.NewGuid(),
                    QueueEntryId = queueEntryId,
                    OldStatus = queueEntry.Status,
                    NewStatus = queueEntry.Status, // Status remains the same but we want to log the action
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = staffId,
                    Reason = "Priority given by staff"
                };

                await _queueRepository.AddStatusChangeAsync(statusChange);
                await _queueRepository.UpdateAsync(queueEntry);

                // Reorder the rest of the queue
                await ReorderQueueAsync(queueEntry.OutletId);

                // Notify clients about queue update
                await _queueHub.NotifyQueueUpdated(queueEntry.OutletId);

                // Update wait time estimates
                await UpdateWaitTimesAsync(queueEntry.OutletId);

                // Return updated entry
                var outlet = await _outletService.GetOutletByIdAsync(queueEntry.OutletId);
                return await MapToQueueEntryDtoAsync(queueEntry, outlet?.Name ?? "Unknown Outlet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error prioritizing held entry: {QueueEntryId}", queueEntryId);
                throw;
            }
        }

        public async Task ReorderQueueAsync(Guid outletId, Guid? processedQueueEntryId = null)
        {
            _logger.LogInformation("Reordering queue for outlet: {OutletId}", outletId);

            try
            {
                // Get all active entries (Waiting and Held)
                var activeEntries = await _queueRepository.GetByOutletIdAsync(outletId, "Waiting");

                // Order entries (non-held first, then by original position)
                var orderedEntries = activeEntries
                    .OrderBy(e => e.IsHeld)
                    .ThenBy(e => e.QueuePosition)
                    .ToList();

                // Reassign positions
                for (int i = 0; i < orderedEntries.Count; i++)
                {
                    orderedEntries[i].QueuePosition = i + 1;
                    await _queueRepository.UpdateAsync(orderedEntries[i]);
                }

                // Update wait time estimates
                await UpdateWaitTimesAsync(outletId);

                // Notify clients about queue update via SignalR
                await _queueHub.NotifyQueueUpdated(outletId);

                // Send WhatsApp notifications to all affected customers
                if (processedQueueEntryId.HasValue)
                {
                    await SendQueueUpdatesToAllAffectedCustomersAsync(outletId, processedQueueEntryId.Value);
                }
                else
                {
                    // If no specific entry was processed, update all waiting entries
                    await SendQueueUpdatesToAllAffectedCustomersAsync(outletId, Guid.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering queue for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task UpdateWaitTimesAsync(Guid outletId)
        {
            _logger.LogInformation("Updating wait times for outlet: {OutletId}", outletId);

            try
            {
                // Get all waiting entries
                var waitingEntries = await _queueRepository.GetByOutletIdAsync(outletId, "Waiting");
                if (!waitingEntries.Any())
                {
                    _logger.LogInformation("No waiting entries for outlet: {OutletId}", outletId);
                    return;
                }

                // Update each entry's wait time
                foreach (var entry in waitingEntries)
                {
                    int estimatedWait = await _waitTimeEstimationService.EstimateWaitTimeAsync(
                        outletId, entry.PartySize, entry.QueuePosition);

                    entry.EstimatedWaitMinutes = estimatedWait;
                    await _queueRepository.UpdateAsync(entry);

                    // Notify customer of updated wait time
                    await NotifyCustomerOfQueueStatusUpdateAsync(entry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wait times for outlet: {OutletId}", outletId);
                throw;
            }
        }

        #region Helper Methods
        private async Task<string> GenerateQueueCodeAsync(Guid outletId)
        {
            // Get a unique prefix based on outlet (first 3 characters of Guid should be unique enough)
            string outletPrefix = outletId.ToString().Substring(0, 3).ToUpper();

            // Add today's date code (e.g., 0409 for April 9)
            string dateCode = DateTime.UtcNow.ToString("MMdd");

            // Get today's date to find the highest number for TODAY only
            var today = DateTime.UtcNow.Date;

            // Find the highest existing queue number for this outlet for today only
            var entriesForOutletToday = await _queueRepository.GetByOutletIdAsync(outletId);
            entriesForOutletToday = entriesForOutletToday
                .Where(e => e.CreatedAt.Date == today)
                .ToList();

            int highestNumber = 0;

            // Parse existing queue codes to find the highest number for today
            foreach (var entry in entriesForOutletToday)
            {
                if (entry.QueueCode.StartsWith($"{outletPrefix}-{dateCode}-"))
                {
                    string numberPart = entry.QueueCode.Substring(9); // Skip the "XXX-MMDD-" prefix
                    if (int.TryParse(numberPart, out int queueNumber) && queueNumber > highestNumber)
                    {
                        highestNumber = queueNumber;
                    }
                }
            }

            // Generate new code with format "XXX-MMDD-001" 
            // (e.g., "3F1-0409-001" for April 9, first queue)
            return $"{outletPrefix}-{dateCode}-{(highestNumber + 1):D03}";
        }

        private void ValidateStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, List<string>>
            {
                { "Waiting", new List<string> { "Called", "Cancelled", "NoShow" } },
                { "Called", new List<string> { "Seated", "Cancelled", "NoShow" } },
                { "Seated", new List<string> { "Completed" } }
            };

            if (!validTransitions.ContainsKey(currentStatus) ||
                !validTransitions[currentStatus].Contains(newStatus))
            {
                throw new InvalidOperationException(
                    $"Invalid status transition from '{currentStatus}' to '{newStatus}'");
            }
        }

        private async Task<QueueEntryDto> MapToQueueEntryDtoAsync(QueueEntry queueEntry, string outletName)
        {
            var tableAssignments = new List<TableAssignmentDto>();

            // For each table assignment, get table details and add to list
            foreach (var assignment in queueEntry.TableAssignments)
            {
                var table = await _tableService.GetTableByIdAsync(assignment.TableId);
                if (table != null)
                {
                    tableAssignments.Add(new TableAssignmentDto
                    {
                        TableId = table.Id,
                        TableNumber = table.TableNumber,
                        Section = table.Section,
                        Capacity = table.Capacity,
                        Status = assignment.Status
                    });
                }
            }

            var totalInQueue = await _queueRepository.CountActiveQueueEntriesAsync(queueEntry.OutletId);

            return new QueueEntryDto
            {
                Id = queueEntry.Id,
                QueueCode = queueEntry.QueueCode,
                OutletId = queueEntry.OutletId,
                OutletName = outletName,
                CustomerName = queueEntry.CustomerName,
                CustomerPhone = queueEntry.CustomerPhone,
                PartySize = queueEntry.PartySize,
                SpecialRequests = queueEntry.SpecialRequests,
                Status = queueEntry.Status,
                QueuePosition = queueEntry.QueuePosition,
                QueuedAt = queueEntry.QueuedAt,
                CalledAt = queueEntry.CalledAt,
                SeatedAt = queueEntry.SeatedAt,
                CompletedAt = queueEntry.CompletedAt,
                EstimatedWaitMinutes = queueEntry.EstimatedWaitMinutes,
                IsHeld = queueEntry.IsHeld,
                HeldSince = queueEntry.HeldSince,
                TableAssignments = tableAssignments
            };
        }

        private async Task NotifyCustomerOfQueueStatusUpdateAsync(QueueEntry queueEntry)
        {
            var totalInQueue = await _queueRepository.CountActiveQueueEntriesAsync(queueEntry.OutletId);

            var statusUpdate = new QueueStatusDto
            {
                QueueEntryId = queueEntry.Id,
                QueueCode = queueEntry.QueueCode,
                QueuePosition = queueEntry.QueuePosition,
                Status = queueEntry.Status,
                EstimatedWaitMinutes = queueEntry.EstimatedWaitMinutes,
                TotalInQueue = totalInQueue
            };

            await _queueHub.UpdateQueueStatus(statusUpdate);
        }

        private async Task SendQueueUpdatesToAllAffectedCustomersAsync(Guid outletId, Guid processedQueueEntryId)
        {
            _logger.LogInformation("Sending queue updates to all affected customers for outlet: {OutletId}", outletId);

            try
            {
                // Get all active waiting entries in the queue for this outlet
                var waitingEntries = await _queueRepository.GetByOutletIdAsync(outletId, "Waiting");

                // Filter out the entry that was just processed (unless it's Guid.Empty)
                var entriesNeedingUpdates = processedQueueEntryId != Guid.Empty
                    ? waitingEntries.Where(e => e.Id != processedQueueEntryId).ToList()
                    : waitingEntries.ToList();

                entriesNeedingUpdates = entriesNeedingUpdates.OrderBy(e => e.QueuePosition).ToList();

                if (!entriesNeedingUpdates.Any())
                {
                    _logger.LogInformation("No waiting customers to update for outlet: {OutletId}", outletId);
                    return;
                }

                _logger.LogInformation("Sending updates to {Count} customers", entriesNeedingUpdates.Count);

                // Send updates to each customer
                foreach (var entry in entriesNeedingUpdates)
                {
                    try
                    {
                        // Send individual notification to each customer
                        await _notificationService.SendQueueUpdateAsync(entry.Id);

                        // Small delay to prevent flooding the notification system
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other notifications
                        _logger.LogError(ex, "Error sending queue update to entry: {QueueEntryId}", entry.Id);
                    }
                }

                _logger.LogInformation("Successfully sent queue updates to all affected customers for outlet: {OutletId}", outletId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue updates to affected customers for outlet: {OutletId}", outletId);
                throw;
            }
        }
        #endregion
    }
}