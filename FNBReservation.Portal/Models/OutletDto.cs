using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class OutletDto
    {
        [JsonPropertyName("outlet_id")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("operatingHours")]
        public OperatingHours OperatingHours { get; set; } = new OperatingHours();

        [JsonPropertyName("tables")]
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();

        [JsonPropertyName("maxAdvanceReservationTime")]
        public int MaxAdvanceReservationTime { get; set; } = 30; // Default 30 days

        [JsonPropertyName("minAdvanceReservationTime")]
        public int MinAdvanceReservationTime { get; set; } = 1; // Default 1 hour

        [JsonPropertyName("contact")]
        public ContactInfo Contact { get; set; } = new ContactInfo();

        [JsonPropertyName("queueEnabled")]
        public bool QueueEnabled { get; set; } = true;

        [JsonPropertyName("specialRequirements")]
        public List<string> SpecialRequirements { get; set; } = new List<string>();

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";
    }

    public class OperatingHours
    {
        [JsonPropertyName("monday")]
        public DaySchedule Monday { get; set; } = new DaySchedule();

        [JsonPropertyName("tuesday")]
        public DaySchedule Tuesday { get; set; } = new DaySchedule();

        [JsonPropertyName("wednesday")]
        public DaySchedule Wednesday { get; set; } = new DaySchedule();

        [JsonPropertyName("thursday")]
        public DaySchedule Thursday { get; set; } = new DaySchedule();

        [JsonPropertyName("friday")]
        public DaySchedule Friday { get; set; } = new DaySchedule();

        [JsonPropertyName("saturday")]
        public DaySchedule Saturday { get; set; } = new DaySchedule();

        [JsonPropertyName("sunday")]
        public DaySchedule Sunday { get; set; } = new DaySchedule();
    }

    public class DaySchedule
    {
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; } = true;

        [JsonPropertyName("openTime")]
        public string OpenTime { get; set; } = "09:00";

        [JsonPropertyName("closeTime")]
        public string CloseTime { get; set; } = "22:00";

        [JsonPropertyName("breakTimes")]
        public List<BreakTime> BreakTimes { get; set; } = new List<BreakTime>();
    }

    public class BreakTime
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;
    }

    public class TableInfo
    {
        [JsonPropertyName("tableId")]
        public string TableId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; } = 2;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Available";

        [JsonPropertyName("location")]
        public string Location { get; set; } = "Main";
    }

    public class ContactInfo
    {
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("website")]
        public string Website { get; set; } = string.Empty;
    }
}