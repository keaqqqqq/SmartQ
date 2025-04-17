using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace FNBReservation.Portal.Services
{
    public class CurrentUserService
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(
            AuthenticationStateProvider authStateProvider,
            IJSRuntime jsRuntime,
            ILogger<CurrentUserService> logger)
        {
            _authStateProvider = authStateProvider;
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task<string> GetCurrentUsernameAsync()
        {
            try
            {
                // First try to get from authentication state (most reliable)
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    // Try name claim first
                    var nameClaim = user.FindFirst(ClaimTypes.Name);
                    if (nameClaim != null && !string.IsNullOrEmpty(nameClaim.Value))
                    {
                        return nameClaim.Value;
                    }

                    // Then try username claim
                    var usernameClaim = user.FindFirst("username") ?? user.FindFirst(ClaimTypes.NameIdentifier);
                    if (usernameClaim != null && !string.IsNullOrEmpty(usernameClaim.Value))
                    {
                        return usernameClaim.Value;
                    }
                }

                // If auth state doesn't have it, try localStorage
                try
                {
                    var localUsername = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentUser");
                    if (!string.IsNullOrEmpty(localUsername))
                    {
                        return localUsername;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing localStorage for username");
                }

                // Fall back to a default value if necessary
                return "Admin";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining current username");
                return "Admin"; // Default fallback value
            }
        }

        public async Task<string> GetCurrentRoleAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var roleClaim = user.FindFirst(ClaimTypes.Role);
                    if (roleClaim != null && !string.IsNullOrEmpty(roleClaim.Value))
                    {
                        return roleClaim.Value;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user role");
                return null;
            }
        }
        
        public async Task<string> GetCurrentOutletIdAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var outletIdClaim = user.FindFirst("OutletId");
                    if (outletIdClaim != null && !string.IsNullOrEmpty(outletIdClaim.Value))
                    {
                        return outletIdClaim.Value;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user outlet ID");
                return null;
            }
        }
    }
} 