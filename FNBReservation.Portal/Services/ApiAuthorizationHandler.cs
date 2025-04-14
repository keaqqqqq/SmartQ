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
        private readonly JwtTokenService _tokenService;
        private readonly ILogger<ApiAuthorizationHandler> _logger;

        public ApiAuthorizationHandler(JwtTokenService tokenService, ILogger<ApiAuthorizationHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // If the request doesn't already have an Authorization header, add it
                if (!request.Headers.Contains("Authorization"))
                {
                    string token = await _tokenService.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }

                // Send the request
                var response = await base.SendAsync(request, cancellationToken);

                // If we got a 401 Unauthorized response, try to refresh the token and retry
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Received 401 Unauthorized response. Attempting to refresh token.");

                    var refreshResult = await _tokenService.RefreshTokenAsync();
                    if (refreshResult.Success)
                    {
                        _logger.LogInformation("Token refreshed successfully. Retrying the original request.");

                        // Clone the original request
                        var newRequest = CloneHttpRequestMessage(request);
                        
                        // Update the Authorization header with the new token
                        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);
                        
                        // Retry the request
                        return await base.SendAsync(newRequest, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to refresh token: {ErrorMessage}", refreshResult.ErrorMessage);
                    }
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