using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FNBReservation.Portal.Models;
using Microsoft.Extensions.Configuration;

namespace FNBReservation.Portal.Services
{
    public interface IStaffService
    {
        Task<List<StaffMember>> GetStaffByOutletAsync(int outletId);
        Task<StaffMember> GetStaffByIdAsync(int id);
        Task<StaffMember> CreateStaffAsync(StaffCreateRequest request);
        Task<StaffMember> UpdateStaffAsync(StaffUpdateRequest request);
        Task DeleteStaffAsync(int id);
    }

    public interface IOutletService
    {
        Task<List<Outlet>> GetOutletsAsync();
        Task<Outlet> GetOutletByIdAsync(int id);
    }

    public class StaffService : IStaffService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl;

        public StaffService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseApiUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<StaffMember>> GetStaffByOutletAsync(int outletId)
        {
            return await _httpClient.GetFromJsonAsync<List<StaffMember>>($"{_baseApiUrl}/api/staff/outlet/{outletId}");
        }

        public async Task<StaffMember> GetStaffByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<StaffMember>($"{_baseApiUrl}/api/staff/{id}");
        }

        public async Task<StaffMember> CreateStaffAsync(StaffCreateRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseApiUrl}/api/staff", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StaffMember>();
        }

        public async Task<StaffMember> UpdateStaffAsync(StaffUpdateRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseApiUrl}/api/staff/{request.Id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StaffMember>();
        }

        public async Task DeleteStaffAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseApiUrl}/api/staff/{id}");
            response.EnsureSuccessStatusCode();
        }
    }

    public class OutletService : IOutletService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl;

        public OutletService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseApiUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Outlet>> GetOutletsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Outlet>>($"{_baseApiUrl}/api/outlets");
        }

        public async Task<Outlet> GetOutletByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Outlet>($"{_baseApiUrl}/api/outlets/{id}");
        }
    }
}