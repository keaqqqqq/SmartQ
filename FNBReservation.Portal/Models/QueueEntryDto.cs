using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class QueueEntryDto
    {
        [JsonPropertyName("id")]
        public string QueueId { get; set; }
        
        [JsonPropertyName("queueCode")]
        public string QueueCode { get; set; }
        
        [JsonPropertyName("outletId")]
        public string OutletId { get; set; }
        
        [JsonPropertyName("outletName")]
        public string OutletName { get; set; }
        
        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; }
        
        [JsonPropertyName("customerPhone")]
        public string CustomerPhone { get; set; }
        
        [JsonPropertyName("partySize")]
        public int PartySize { get; set; }
        
        [JsonPropertyName("specialRequests")]
        public string Notes { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("queuePosition")]
        public int QueuePosition { get; set; }
        
        [JsonPropertyName("queuedAt")]
        public DateTime QueuedAt { get; set; }
        
        [JsonPropertyName("calledAt")]
        public DateTime? CalledAt { get; set; }
        
        [JsonPropertyName("seatedAt")]
        public DateTime? SeatedAt { get; set; }
        
        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }
        
        [JsonPropertyName("estimatedWaitMinutes")]
        public int EstimatedWaitMinutes { get; set; }
        
        [JsonPropertyName("isHeld")]
        public bool IsHeld { get; set; }
        
        [JsonPropertyName("heldSince")]
        public DateTime? HeldSince { get; set; }
        
        [JsonPropertyName("assignedTableId")]
        public string AssignedTableId { get; set; }
        
        [JsonPropertyName("tableAssignments")]
        public List<QueueTableAssignment> TableAssignments { get; set; } = new List<QueueTableAssignment>();
        
        [JsonIgnore]
        public string AssignedTableNumber => TableAssignments?.Count > 0 ? TableAssignments[0].TableNumber : string.Empty;
    }

    public class QueueTableAssignment
    {
        [JsonPropertyName("tableId")]
        public string TableId { get; set; }
        
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
        
        [JsonPropertyName("section")]
        public string Section { get; set; }
        
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
} 