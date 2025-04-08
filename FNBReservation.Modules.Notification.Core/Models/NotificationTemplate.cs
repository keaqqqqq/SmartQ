// NotificationTemplate.cs
namespace FNBReservation.Modules.Notification.Core.Models
{
    public class NotificationTemplate
    {
        public string TemplateName { get; set; }
        public string TemplateContent { get; set; }
        public List<string> ParameterNames { get; set; } = new List<string>();
    }
}