using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabModule.Models;

namespace LabModule.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            try
            {
                var data = new { email, password };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/auth/login", content);
                var json = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(json);
                }
                throw new Exception($"Ошибка API: {json}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось подключиться к API: {ex.Message}");
            }
        }

        public async Task<User> RegisterAsync(string email, string password, string fullName, string role)
        {
            var data = new { email, password, fullName, role };
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/register", content);
            var json = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<User>(json);
            }
            throw new Exception(json);
        }

        public async Task<List<Batch>> GetBatchesAsync()
        {
            var response = await _httpClient.GetAsync("api/batches");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Batch>>(json) ?? new List<Batch>();
        }

        public async Task<List<QualityControl>> GetQualityControlsAsync()
        {
            var response = await _httpClient.GetAsync("api/quality-controls");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<QualityControl>>(json) ?? new List<QualityControl>();
        }

        public async Task<QualityControl> CreateQualityControlAsync(QualityControl control)
        {
            var content = new StringContent(JsonSerializer.Serialize(control), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/quality-controls", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<QualityControl>(json);
        }

        public async Task<List<Deviation>> GetDeviationsAsync()
        {
            var response = await _httpClient.GetAsync("api/deviations");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Deviation>>(json) ?? new List<Deviation>();
        }

        public async Task<Deviation> CreateDeviationAsync(Deviation deviation)
        {
            var content = new StringContent(JsonSerializer.Serialize(deviation), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/deviations", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Deviation>(json);
        }

        public async Task<List<Notification>> GetNotificationsAsync()
        {
            var response = await _httpClient.GetAsync("api/notifications");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var batches = await GetBatchesAsync();
                var controls = await GetQualityControlsAsync();
                var deviations = await GetDeviationsAsync();
                
                var stats = new DashboardStats();
                stats.PendingBatchesCount = 0;
                stats.CompletedBatchesCount = 0;
                stats.TotalTestsCount = controls.Count;
                stats.DeviationsCount = deviations.Count;
                
                foreach (var b in batches)
                {
                    if (b.Status == "completed" || b.Status == "running")
                        stats.PendingBatchesCount++;
                    if (b.Status == "completed")
                        stats.CompletedBatchesCount++;
                }
                
                return stats;
            }
            catch
            {
                return new DashboardStats();
            }
        }
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public User User { get; set; }
    }
}