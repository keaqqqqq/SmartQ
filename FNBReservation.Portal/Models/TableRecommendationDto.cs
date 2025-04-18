using System;
using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class TableRecommendationDto
    {
        [JsonPropertyName("tableId")]
        public string TableId { get; set; }
        
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
        
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("recommendationScore")]
        public double RecommendationScore { get; set; }
        
        [JsonPropertyName("location")]
        public string Location { get; set; }
        
        [JsonPropertyName("matchReason")]
        public string MatchReason { get; set; }
    }
} 