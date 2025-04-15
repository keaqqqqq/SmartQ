using System.Text.Json;
using Microsoft.JSInterop;

namespace FNBReservation.Portal.Services
{
    public class JwtTokenService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";
        private const string UserInfoKey = "user_info";

        public JwtTokenService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SaveTokensAsync(string accessToken, string refreshToken)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AccessTokenKey);
        }

        public async Task<string> GetRefreshTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
        }

        public async Task ClearTokensAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserInfoKey);
        }

        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                var token = await GetAccessTokenAsync();
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        public async Task SaveUserInfoAsync(UserInfo userInfo)
        {
            var json = JsonSerializer.Serialize(userInfo);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserInfoKey, json);
        }

        public async Task<UserInfo> GetUserInfoAsync()
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", UserInfoKey);
            if (string.IsNullOrEmpty(json))
                return new UserInfo();

            try
            {
                return JsonSerializer.Deserialize<UserInfo>(json) ?? new UserInfo();
            }
            catch
            {
                return new UserInfo();
            }
        }
    }
} 