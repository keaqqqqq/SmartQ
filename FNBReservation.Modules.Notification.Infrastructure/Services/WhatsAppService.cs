// WhatsAppService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Notification.Core.Interfaces;
using FNBReservation.Modules.Notification.Core.Models;

namespace FNBReservation.Modules.Notification.Infrastructure.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _apiBaseUrl;
        private readonly string _apiToken;
        private readonly string _fromPhoneNumberId;
        private readonly string _messageTemplate;

        public WhatsAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Load configuration
            var whatsAppSettings = _configuration.GetSection("WhatsAppApi");
            _apiBaseUrl = whatsAppSettings["BaseUrl"] ?? "https://graph.facebook.com/v22.0";
            _apiToken = whatsAppSettings["Token"] ?? throw new ArgumentNullException("WhatsApp API Token is not configured");
            _fromPhoneNumberId = whatsAppSettings["PhoneNumberId"] ?? throw new ArgumentNullException("WhatsApp Phone Number ID is not configured");
            _messageTemplate = whatsAppSettings["TemplateNamespace"] ?? "whatsapp_business_account";

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task SendMessageAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("Sending WhatsApp message to {PhoneNumber}", phoneNumber);

            try
            {
                // Format phone number (remove any spaces, dashes, and ensure it has country code)
                string formattedPhone = FormatPhoneNumber(phoneNumber);

                // Prepare the message payload
                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = formattedPhone,
                    type = "text",
                    text = new
                    {
                        preview_url = false,
                        body = message
                    }
                };

                // Serialize the payload
                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the request
                var endpoint = $"/{_fromPhoneNumberId}/messages";
                var response = await _httpClient.PostAsync(endpoint, httpContent);

                // Handle the response
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("WhatsApp message sent successfully. Response: {Response}", responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send WhatsApp message. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new Exception($"WhatsApp API returned {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        public async Task SendTemplateMessageAsync(string phoneNumber, string templateName, List<object> templateParams)
        {
            _logger.LogInformation("Sending WhatsApp template message to {PhoneNumber} using template {TemplateName}",
                phoneNumber, templateName);

            try
            {
                // Format phone number
                string formattedPhone = FormatPhoneNumber(phoneNumber);

                // Prepare components
                var components = new List<object>();
                if (templateParams != null && templateParams.Count > 0)
                {
                    var parameters = new List<object>();
                    foreach (var param in templateParams)
                    {
                        parameters.Add(new { type = "text", text = param.ToString() });
                    }

                    components.Add(new { type = "body", parameters = parameters });
                }

                // Prepare the message payload for template
                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = formattedPhone,
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = "en_US" },
                        components = components
                    }
                };

                // Serialize the payload
                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the request
                var endpoint = $"/{_fromPhoneNumberId}/messages";
                var response = await _httpClient.PostAsync(endpoint, httpContent);

                // Handle the response
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("WhatsApp template message sent successfully. Response: {Response}", responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send WhatsApp template message. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new Exception($"WhatsApp API returned {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp template message to {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove any spaces, dashes, parentheses
            string cleaned = new string(phoneNumber.Where(c => char.IsDigit(c)).ToArray());

            // Ensure it has the country code
            if (!cleaned.StartsWith("1") && !cleaned.StartsWith("60") && !cleaned.StartsWith("+"))
            {
                // Default to adding Malaysia country code if not specified
                cleaned = "60" + cleaned;
            }
            else if (cleaned.StartsWith("+"))
            {
                // Remove the plus sign as WhatsApp API doesn't expect it
                cleaned = cleaned.Substring(1);
            }

            return cleaned;
        }
    }
}