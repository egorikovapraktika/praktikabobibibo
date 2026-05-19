using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LabModule.Data;
using LabModule.Models;

namespace LabModule.Services
{
    public class DbService
    {
        private readonly AppDbContext _context;

        public DbService()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(@"Server=KAB17-07\SQLEXPRESS;Database=dbpraktikchka;Trusted_Connection=True;");
            _context = new AppDbContext(optionsBuilder.Options);
        }

        // АВТОРИЗАЦИЯ
        public async Task<User> LoginAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Пользователь не найден");
            return user;
        }

        // РЕГИСТРАЦИЯ
        public async Task<User> RegisterAsync(string email, string password, string fullName, string role)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                throw new Exception("Пользователь с таким email уже существует!");

            int roleId = 3;
            if (role == "technologist") roleId = 1;
            if (role == "operator") roleId = 2;
            if (role == "laboratory") roleId = 3;
            if (role == "admin") roleId = 4;

            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                PasswordHash = password,
                FullName = fullName,
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // ПАРТИИ
        public async Task<List<Batch>> GetBatchesAsync()
        {
            return await _context.Batches.OrderByDescending(b => b.Id).ToListAsync();
        }

        public async Task<Batch> GetBatchByIdAsync(int id)
        {
            return await _context.Batches.FindAsync(id);
        }

        // КОНТРОЛЬ КАЧЕСТВА
        public async Task<List<QualityControl>> GetQualityControlsAsync(int? batchId = null)
        {
            var query = _context.QualityControls.AsQueryable();
            if (batchId.HasValue)
                query = query.Where(q => q.BatchId == batchId);
            return await query.OrderByDescending(q => q.AnalysisDate).ToListAsync();
        }

        public async Task<QualityControl> GetQualityControlByIdAsync(int id)
        {
            return await _context.QualityControls.FindAsync(id);
        }

        public async Task<QualityControl> CreateQualityControlAsync(QualityControl control)
        {
            try
            {
                control.AnalysisDate = DateTime.Now;
                // Используем значения, которые допускает CHECK CONSTRAINT
                control.Result = "fail";      // допустимые: 'pass', 'fail'
                control.Decision = "blocked"; // допустимые: 'approved', 'blocked'
                control.AnalystId = 1;
                
                _context.QualityControls.Add(control);
                await _context.SaveChangesAsync();
                return control;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка БД: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<QualityControl> UpdateQualityControlAsync(QualityControl control)
        {
            var existing = await _context.QualityControls.FindAsync(control.Id);
            if (existing != null)
            {
                existing.MeasuredValue = control.MeasuredValue;
                existing.StandardValue = control.StandardValue;
                existing.SampleType = control.SampleType;
                existing.ParameterName = control.ParameterName;
                existing.Result = control.Result;
                existing.Decision = control.Decision;
                existing.AnalystComment = control.AnalystComment;
                existing.AnalysisDate = control.AnalysisDate;
                
                await _context.SaveChangesAsync();
            }
            return control;
        }

        // ОТКЛОНЕНИЯ
        public async Task<List<Deviation>> GetDeviationsAsync()
        {
            return await _context.Deviations.OrderByDescending(d => d.CreatedAt).ToListAsync();
        }

        public async Task<Deviation> CreateDeviationAsync(Deviation deviation)
        {
            deviation.CreatedAt = DateTime.Now;
            _context.Deviations.Add(deviation);
            await _context.SaveChangesAsync();
            return deviation;
        }

        // УВЕДОМЛЕНИЯ
        public async Task<List<Notification>> GetNotificationsAsync()
        {
            return await _context.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        // СТАТИСТИКА
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var batches = await GetBatchesAsync();
            var controls = await GetQualityControlsAsync();
            var deviations = await GetDeviationsAsync();
            
            return new DashboardStats
            {
                PendingBatchesCount = batches.Count(b => b.Status == "completed" || b.Status == "running"),
                CompletedBatchesCount = batches.Count(b => b.Status == "completed"),
                TotalTestsCount = controls.Count,
                DeviationsCount = deviations.Count
            };
        }

        // ПРОДУКТЫ
        public async Task<List<Product>> GetProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }
    }
}