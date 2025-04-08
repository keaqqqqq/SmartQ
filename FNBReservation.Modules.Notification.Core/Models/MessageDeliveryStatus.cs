// MessageDeliveryStatus.cs
namespace FNBReservation.Modules.Notification.Core.Models
{
    public class MessageDeliveryStatus
    {
        public bool IsSuccessful { get; set; }
        public string MessageId { get; set; }
        public string Channel { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }
}