using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Portal.Services;

namespace FNBReservation.Portal.Services
{
    public class ApiAuthorizationHandler : DelegatingHandler
    {
        private readonly AuthService _authService;
        private readonly ILogger<ApiAuthorizationHandler> _logger;

        public ApiAuthorizationHandler(AuthService authService, ILogger<ApiAuthorizationHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Since we're using HTTP-only cookies, we don't need to add Authorization headers
                // Cookies are automatically included in the request
                
                // Send the request
                var response = await base.SendAsync(request, cancellationToken);

                // If we got a 401 Unauthorized response, try to refresh the token and retry
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Received 401 Unauthorized response. Attempting to refresh token.");

                    // Call RefreshToken without capturing the return value as it returns void
                    await _authService.RefreshToken();
                    
                    // Clone the original request
                    var newRequest = CloneHttpRequestMessage(request);
                    
                    // Retry the request - cookies will be included automatically
                    return await base.SendAsync(newRequest, cancellationToken);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in API authorization handler");
                throw;
            }
        }

        private HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Copy properties
            clone.Version = request.Version;

            // Copy headers
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy content if present
            if (request.Content != null)
            {
                // Try to clone the content if possible
                byte[] contentBytes = null;
                
                // We can't directly get request.Content.ReadAsByteArrayAsync() because it might have been read
                // So we need to create a new StringContent or other appropriate content type
                
                // For this implementation, we'll store content in memory which works for most API scenarios
                // For large files, a more sophisticated solution would be needed
                if (request.Content is StringContent)
                {
                    var originalContent = request.Content.ReadAsStringAsync().Result;
                    clone.Content = new StringContent(originalContent);
                    
                    // Preserve content headers
                    foreach (var header in request.Content.Headers)
                    {
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                else
                {
                    // For other content types, we'll need to handle them separately
                    // This is a simplified implementation
                    contentBytes = request.Content.ReadAsByteArrayAsync().Result;
                    clone.Content = new ByteArrayContent(contentBytes);
                    
                    // Preserve content headers
                    foreach (var header in request.Content.Headers)
                    {
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            return clone;
        }
    }
} 