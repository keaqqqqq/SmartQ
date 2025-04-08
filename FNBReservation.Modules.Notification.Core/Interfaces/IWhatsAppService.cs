// IWhatsAppService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNBReservation.Modules.Notification.Core.Interfaces
{
    public interface IWhatsAppService
    {
        /// <summary>
        /// Sends a plain text message to a WhatsApp number
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number</param>
        /// <param name="message">The message text to send</param>
        Task SendMessageAsync(string phoneNumber, string message);

        /// <summary>
        /// Sends a template-based message to a WhatsApp number
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number</param>
        /// <param name="templateName">The name of the pre-approved WhatsApp template</param>
        /// <param name="templateParams">The parameters to populate the template with</param>
        Task SendTemplateMessageAsync(string phoneNumber, string templateName, List<object> templateParams);
    }
}