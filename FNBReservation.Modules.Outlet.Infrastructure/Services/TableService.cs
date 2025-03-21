// FNBReservation.Modules.Outlet.Infrastructure/Services/TableService.cs (Updated)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Entities;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.Infrastructure.Services
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _tableRepository;
        private readonly IOutletRepository _outletRepository;
        private readonly ILogger<TableService> _logger;

        public TableService(
            ITableRepository tableRepository,
            IOutletRepository outletRepository,
            ILogger<TableService> logger)
        {
            _tableRepository = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository));
            _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TableDto> CreateTableAsync(Guid outletId, CreateTableDto createTableDto, Guid userId)
        {
            _logger.LogInformation("Creating table {TableNumber} for outlet {OutletId}",
                createTableDto.TableNumber, outletId);

            // Validate outlet exists
            var outlet = await _outletRepository.GetByIdAsync(outletId);
            if (outlet == null)
            {
                _logger.LogWarning("Outlet {OutletId} not found", outletId);
                throw new ArgumentException($"Outlet with ID {outletId} not found");
            }

            // Create table entity
            var table = new TableEntity
            {
                Id = Guid.NewGuid(),
                OutletId = outletId,
                TableNumber = createTableDto.TableNumber,
                Capacity = createTableDto.Capacity,
                Section = createTableDto.Section,
                IsActive = createTableDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            // Save to database
            var createdTable = await _tableRepository.CreateAsync(table);

            // Map to DTO
            return MapToTableDto(createdTable);
        }

        public async Task<TableDto> GetTableByIdAsync(Guid tableId)
        {
            _logger.LogInformation("Getting table by ID {TableId}", tableId);

            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null)
            {
                _logger.LogWarning("Table {TableId} not found", tableId);
                return null;
            }

            return MapToTableDto(table);
        }

        public async Task<IEnumerable<TableDto>> GetTablesByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting all tables for outlet {OutletId}", outletId);

            var tables = await _tableRepository.GetByOutletIdAsync(outletId);
            return tables.Select(MapToTableDto);
        }

        public async Task<IEnumerable<TableDto>> GetReservationOnlyTablesAsync(Guid outletId)
        {
            _logger.LogWarning("GetReservationOnlyTablesAsync is deprecated as ReservationOnly field has been removed");

            // Just return all active tables since we no longer have reservation-only tables
            var tables = await _tableRepository.GetByOutletIdAsync(outletId);
            return tables.Where(t => t.IsActive).Select(MapToTableDto);
        }

        public async Task<IEnumerable<SectionDto>> GetSectionsByOutletIdAsync(Guid outletId)
        {
            _logger.LogInformation("Getting sections for outlet {OutletId}", outletId);

            var sections = await _tableRepository.GetSectionsByOutletIdAsync(outletId);
            var sectionDtos = new List<SectionDto>();

            foreach (var section in sections)
            {
                var tableCount = await _tableRepository.GetTableCountBySectionAsync(outletId, section);
                var totalCapacity = await _tableRepository.GetTotalCapacityBySectionAsync(outletId, section);

                sectionDtos.Add(new SectionDto
                {
                    Name = section,
                    TableCount = tableCount,
                    TotalCapacity = totalCapacity
                });
            }

            return sectionDtos;
        }

        public async Task<TableDto> UpdateTableAsync(Guid tableId, UpdateTableDto updateTableDto, Guid userId)
        {
            _logger.LogInformation("Updating table {TableId}", tableId);

            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null)
            {
                _logger.LogWarning("Table {TableId} not found for update", tableId);
                return null;
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(updateTableDto.TableNumber))
                table.TableNumber = updateTableDto.TableNumber;

            if (updateTableDto.Capacity.HasValue)
                table.Capacity = updateTableDto.Capacity.Value;

            if (!string.IsNullOrEmpty(updateTableDto.Section))
                table.Section = updateTableDto.Section;

            if (updateTableDto.IsActive.HasValue)
                table.IsActive = updateTableDto.IsActive.Value;

            table.UpdatedAt = DateTime.UtcNow;
            table.UpdatedBy = userId;

            // Save changes
            var updatedTable = await _tableRepository.UpdateAsync(table);

            return MapToTableDto(updatedTable);
        }

        public async Task<bool> DeleteTableAsync(Guid tableId)
        {
            _logger.LogInformation("Deleting table {TableId}", tableId);

            return await _tableRepository.DeleteAsync(tableId);
        }

        public async Task<int> GetTotalTablesCapacityAsync(Guid outletId)
        {
            _logger.LogInformation("Getting total tables capacity for outlet {OutletId}", outletId);

            return await _tableRepository.GetTotalTablesCapacityAsync(outletId);
        }

        public async Task<int> GetReservationCapacityAsync(Guid outletId)
        {
            _logger.LogInformation("Getting reservation capacity for outlet {OutletId}", outletId);

            return await _tableRepository.GetReservationCapacityAsync(outletId);
        }

        // Helper methods
        private TableDto MapToTableDto(TableEntity entity)
        {
            return new TableDto
            {
                Id = entity.Id,
                OutletId = entity.OutletId,
                TableNumber = entity.TableNumber,
                Capacity = entity.Capacity,
                Section = entity.Section,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}