using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OperatorModule.Data;
using OperatorModule.Models;

namespace OperatorModule.Services
{
    public class DbService
    {
        private readonly AppDbContext _context;
        private User _currentUser;

        public DbService()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(@"Server=KAB17-07\SQLEXPRESS;Database=dbpraktikchka;Trusted_Connection=True;");
            _context = new AppDbContext(optionsBuilder.Options);
        }

        public User CurrentUser => _currentUser;

        // АВТОРИЗАЦИЯ
        public async Task<User> LoginAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("Пользователь не найден");
            
            _currentUser = user;
            return user;
        }

        // РЕГИСТРАЦИЯ
        public async Task<User> RegisterAsync(string email, string password, string fullName, string role)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                throw new Exception("Пользователь с таким email уже существует!");

            int roleId = 2;
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

        // АКТИВНЫЕ ПАРТИИ
        public async Task<List<Batch>> GetActiveBatchesAsync()
        {
            return await _context.Batches
                .Where(b => b.Status == "running" || b.Status == "planned")
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<Batch> GetBatchByIdAsync(int id)
        {
            return await _context.Batches.FindAsync(id);
        }

        // ШАГИ ТЕХКАРТЫ
        public async Task<List<TechMapStep>> GetTechMapStepsAsync(int techMapId)
        {
            return await _context.TechMapSteps
                .Where(s => s.TechMapId == techMapId)
                .OrderBy(s => s.StepOrder)
                .ToListAsync();
        }

        // ШАГИ ПАРТИИ
        public async Task<List<BatchStep>> GetBatchStepsAsync(int batchId)
        {
            return await _context.BatchSteps
                .Where(s => s.BatchId == batchId)
                .OrderBy(s => s.StepOrder)
                .ToListAsync();
        }

        public async Task<BatchStep> GetCurrentBatchStepAsync(int batchId)
        {
            return await _context.BatchSteps
                .FirstOrDefaultAsync(s => s.BatchId == batchId && s.StartedAt != null && s.CompletedAt == null);
        }

        public async Task<BatchStep> StartBatchStepAsync(int batchId, int stepOrder, string stepName, int operatorId)
        {
            var step = new BatchStep
            {
                BatchId = batchId,
                StepOrder = stepOrder,
                StepName = stepName,
                StartedAt = DateTime.Now,
                StartedBy = operatorId,
                DeviationFlag = false
            };
            
            _context.BatchSteps.Add(step);
            await _context.SaveChangesAsync();
            return step;
        }

        public async Task<BatchStep> CompleteBatchStepAsync(int stepId, decimal? actualTemp, decimal? actualPressure, int? actualDuration, string comment)
        {
            var step = await _context.BatchSteps.FindAsync(stepId);
            if (step != null)
            {
                step.ActualTempC = actualTemp;
                step.ActualPressureBar = actualPressure;
                step.ActualDurationMin = actualDuration;
                step.CompletedAt = DateTime.Now;
                step.OperatorComment = comment;
                await _context.SaveChangesAsync();
            }
            return step;
        }

        public async Task<Deviation> ReportDeviationAsync(int batchId, int stepId, string description, string severity)
        {
            var deviation = new Deviation
            {
                BatchId = batchId,
                Description = description,
                Severity = severity,
                IsResolved = false,
                CreatedAt = DateTime.Now
            };
            
            _context.Deviations.Add(deviation);
            
            if (stepId > 0)
            {
                var step = await _context.BatchSteps.FindAsync(stepId);
                if (step != null)
                    step.DeviationFlag = true;
            }
            
            await _context.SaveChangesAsync();
            return deviation;
        }

        public async Task<Batch> CompleteBatchAsync(int batchId)
        {
            var batch = await _context.Batches.FindAsync(batchId);
            if (batch != null)
            {
                batch.Status = "completed";
                batch.EndTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return batch;
        }

        public async Task<Product> GetProductByTechMapIdAsync(int techMapId)
        {
            var techMap = await _context.TechMaps.FindAsync(techMapId);
            if (techMap != null)
            {
                return await _context.Products.FindAsync(techMap.ProductId);
            }
            return null;
        }
    }
}