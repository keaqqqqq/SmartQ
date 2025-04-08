// ISmsService.cs 
using System.Threading.Tasks;

namespace FNBReservation.Modules.Notification.Core.Interfaces
{
    public interface ISmsService
    {
        /// <summary>
        /// Sends a plain text SMS message to a phone number
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number</param>
        /// <param name="message">The message text to send</param>
        Task SendMessageAsync(string phoneNumber, string message);
    }
}