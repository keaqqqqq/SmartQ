// INotificationService.cs
using System.Threading.Tasks;

namespace FNBReservation.Modules.Notification.Core.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification via the preferred channel (WhatsApp, SMS, etc.)
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number</param>
        /// <param name="message">The message text to send</param>
        /// <param name="preferredChannel">The preferred channel (WhatsApp, SMS)</param>
        Task SendNotificationAsync(string phoneNumber, string message, string preferredChannel = "WhatsApp");
    }
}