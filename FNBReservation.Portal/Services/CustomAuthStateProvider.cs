using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;

namespace FNBReservation.Portal.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private readonly IJSRuntime _jsRuntime;

        public CustomAuthStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // During static rendering, just return anonymous
            return Task.FromResult(new AuthenticationState(_anonymous));
        }

        public async Task<ClaimsPrincipal> GetAuthenticatedUserFromStorageAsync()
        {
            try
            {
                // This method is meant to be called after rendering, during OnAfterRenderAsync
                var storedPrincipal = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authData");
                
                await _jsRuntime.InvokeVoidAsync("console.log", "GetAuthState - Stored auth data: " + 
                    (string.IsNullOrEmpty(storedPrincipal) ? "none" : "exists"));
                
                if (string.IsNullOrEmpty(storedPrincipal))
                {
                    return _anonymous;
                }

                var authData = JsonSerializer.Deserialize<AuthData>(storedPrincipal);
                
                if (authData == null)
                {
                    return _anonymous;
                }

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, authData.Username),
                    new Claim(ClaimTypes.Role, authData.Role)
                }, "FNBReservation");

                var user = new ClaimsPrincipal(identity);
                
                // Update the authentication state and notify
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
                
                return user;
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "Error in GetAuthenticatedUserFromStorage: " + ex.Message);
                return _anonymous;
            }
        }

        public void NotifyUserAuthentication(string username, string role)
        {
            try
            {
                // Store the auth data in localStorage
                var authData = new AuthData { Username = username, Role = role };
                var serializedData = JsonSerializer.Serialize(authData);
                
                _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authData", serializedData);
                
                // Create and update the claims identity
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                }, "FNBReservation");
                
                var user = new ClaimsPrincipal(identity);
                
                // Notify the auth state changed
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
                _jsRuntime.InvokeVoidAsync("console.log", "User authenticated: " + username);
            }
            catch (Exception ex)
            {
                _jsRuntime.InvokeVoidAsync("console.log", "Error in NotifyUserAuthentication: " + ex.Message);
            }
        }

        public void NotifyUserLogout()
        {
            try
            {
                // Clear localStorage
                _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authData");
                
                // Update auth state
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                _jsRuntime.InvokeVoidAsync("console.log", "User logged out");
            }
            catch (Exception ex)
            {
                _jsRuntime.InvokeVoidAsync("console.log", "Error in NotifyUserLogout: " + ex.Message);
            }
        }
    }

    public class AuthData
    {
        public string Username { get; set; }
        public string Role { get; set; }
    }
}