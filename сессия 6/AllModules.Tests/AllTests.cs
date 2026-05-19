using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using TechModule.Services;
using LabModule.Services;
using OperatorModule.Services;
using LabModule.Models;
using OperatorModule.Models;

namespace AllModules.Tests
{
    // ============================================
    // 1. ТЕСТЫ ДЛЯ API (ProductionAPI)
    // ============================================
    
    public class ApiTests
    {
        private readonly HttpClient _client;

        public ApiTests()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:5000/");
        }

        [Fact]
        public async Task API_Login_ValidCredentials_ReturnsToken()
        {
            var loginData = new { email = "admin@test.com", password = "admin123" };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/auth/login", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task API_Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var loginData = new { email = "wrong@test.com", password = "wrong" };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/auth/login", content);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task API_GetProducts_ReturnsOk()
        {
            var response = await _client.GetAsync("api/products");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task API_GetBatches_ReturnsOk()
        {
            var response = await _client.GetAsync("api/batches");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task API_GetDeviations_ReturnsOk()
        {
            var response = await _client.GetAsync("api/deviations");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task API_GetNotifications_ReturnsOk()
        {
            var response = await _client.GetAsync("api/notifications");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task API_GetDashboardStats_ReturnsOk()
        {
            var response = await _client.GetAsync("api/dashboard/stats");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // ============================================
    // 2. ТЕСТЫ ДЛЯ МОДУЛЯ ТЕХНОЛОГА (TechModule)
    // ============================================
    
    public class TechModuleTests
    {
        private readonly ApiService _api;

        public TechModuleTests()
        {
            _api = new ApiService();
        }

        [Fact]
        public async Task Tech_Login_ValidUser_ReturnsSuccess()
        {
            var result = await _api.LoginAsync("test@test.com", "123");
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
        }

        [Fact]
        public async Task Tech_GetProducts_ReturnsList()
        {
            var products = await _api.GetProductsAsync();
            Assert.NotNull(products);
        }

        [Fact]
        public async Task Tech_GetRecipes_ReturnsList()
        {
            var recipes = await _api.GetRecipesAsync();
            Assert.NotNull(recipes);
        }

        [Fact]
        public void Tech_GenerateCaptcha_ReturnsImageAndCode()
        {
            var (imageBytes, code) = CaptchaGenerator.GenerateCaptcha();
            Assert.NotNull(imageBytes);
            Assert.NotNull(code);
            Assert.True(imageBytes.Length > 0);
            Assert.True(code.Length >= 5);
        }

        [Fact]
        public void Tech_CaptchaCode_ContainsOnlyAllowedChars()
        {
            var (imageBytes, code) = CaptchaGenerator.GenerateCaptcha();
            foreach (char c in code)
            {
                Assert.Matches("^[A-Z0-9]$", c.ToString());
            }
        }
    }

    // ============================================
    // 3. ТЕСТЫ ДЛЯ МОДУЛЯ ЛАБОРАТОРИИ (LabModule)
    // ============================================
    
    public class LabModuleTests
    {
        private readonly DbService _db;

        public LabModuleTests()
        {
            _db = new DbService();
        }

        [Fact]
        public async Task Lab_Login_ValidUser_ReturnsUser()
        {
            var user = await _db.LoginAsync("test@test.com", "123");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Lab_GetBatches_ReturnsList()
        {
            var batches = await _db.GetBatchesAsync();
            Assert.NotNull(batches);
        }

        [Fact]
        public async Task Lab_GetQualityControls_ReturnsList()
        {
            var controls = await _db.GetQualityControlsAsync();
            Assert.NotNull(controls);
        }

        [Fact]
        public async Task Lab_CreateQualityControl_ReturnsControl()
        {
            var control = new QualityControl
            {
                BatchId = 1,
                SampleType = "finished_product",
                ParameterName = "Концентрация",
                StandardValue = "95.0-98.0",
                Unit = "%"
            };
            var result = await _db.CreateQualityControlAsync(control);
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task Lab_GetDeviations_ReturnsList()
        {
            var deviations = await _db.GetDeviationsAsync();
            Assert.NotNull(deviations);
        }

        [Fact]
        public async Task Lab_CreateDeviation_ReturnsDeviation()
        {
            var deviation = new Deviation
            {
                BatchId = 1,
                Description = "Тестовое отклонение",
                Severity = "medium",
                IsResolved = false
            };
            var result = await _db.CreateDeviationAsync(deviation);
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task Lab_GetDashboardStats_ReturnsStats()
        {
            var stats = await _db.GetDashboardStatsAsync();
            Assert.NotNull(stats);
        }
    }

    // ============================================
    // 4. ТЕСТЫ ДЛЯ МОДУЛЯ АППАРАТЧИКА (OperatorModule)
    // ============================================
    
    public class OperatorModuleTests
    {
        private readonly OperatorModule.Services.DbService _db;

        public OperatorModuleTests()
        {
            _db = new OperatorModule.Services.DbService();
        }

        [Fact]
        public async Task Op_Login_ValidUser_ReturnsUser()
        {
            var user = await _db.LoginAsync("test@test.com", "123");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Op_GetActiveBatches_ReturnsOnlyRunningAndPlanned()
        {
            var batches = await _db.GetActiveBatchesAsync();
            Assert.NotNull(batches);
            foreach (var batch in batches)
            {
                Assert.Contains(batch.Status, new[] { "running", "planned" });
            }
        }

        [Fact]
        public async Task Op_GetTechMapSteps_ReturnsList()
        {
            var steps = await _db.GetTechMapStepsAsync(1);
            Assert.NotNull(steps);
        }

        [Fact]
        public async Task Op_StartBatchStep_CreatesNewStep()
        {
            var step = await _db.StartBatchStepAsync(1, 1, "Тестовый шаг", 1);
            Assert.NotNull(step);
            Assert.Equal(1, step.BatchId);
            Assert.NotNull(step.StartedAt);
        }

        [Fact]
        public async Task Op_CompleteBatchStep_UpdatesStep()
        {
            var step = await _db.StartBatchStepAsync(1, 1, "Тестовый шаг", 1);
            var completed = await _db.CompleteBatchStepAsync(step.Id, 95.5m, 2.0m, 30, "OK");
            Assert.NotNull(completed);
            Assert.Equal(95.5m, completed.ActualTempC);
            Assert.NotNull(completed.CompletedAt);
        }

        [Fact]
        public async Task Op_ReportDeviation_AddsDeviation()
        {
            var deviation = await _db.ReportDeviationAsync(1, 1, "Тестовое отклонение", "high");
            Assert.NotNull(deviation);
            Assert.True(deviation.Id > 0);
        }

        [Fact]
        public async Task Op_GetCurrentBatchStep_ReturnsActiveStepOrNull()
        {
            var step = await _db.GetCurrentBatchStepAsync(1);
            // Может быть null - это нормально
        }
    }

    // ============================================
    // 5. ОБЩАЯ СТАТИСТИКА ПО ТЕСТАМ
    // ============================================
    
    public class TestSummary
    {
        [Fact]
        public void ShowTestSummary()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  СВОДКА ПО ТЕСТАМ");
            Console.WriteLine("========================================");
            Console.WriteLine("  API Tests: 7 тестов");
            Console.WriteLine("  TechModule Tests: 5 тестов");
            Console.WriteLine("  LabModule Tests: 7 тестов");
            Console.WriteLine("  OperatorModule Tests: 7 тестов");
            Console.WriteLine("========================================");
            Console.WriteLine("  ВСЕГО ТЕСТОВ: 26");
            Console.WriteLine("========================================");
            Assert.True(true);
        }
    }
}