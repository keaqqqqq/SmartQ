using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class TableTypeInfo
    {
        [JsonPropertyName("tableId")]
        public string TableId { get; set; }
        
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
        
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("tableType")]
        public string TableType { get; set; }
        
        [JsonPropertyName("section")]
        public string Section { get; set; }
        
        [JsonPropertyName("location")]
        public string Location { get; set; }
    }
} 