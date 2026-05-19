using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabModule.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("username")]
        public string Username { get; set; }
        
        [Column("email")]
        public string Email { get; set; }
        
        [Column("password_hash")]
        public string PasswordHash { get; set; }
        
        [Column("full_name")]
        public string FullName { get; set; }
        
        [Column("role_id")]
        public int RoleId { get; set; }
        
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("name")]
        public string Name { get; set; }
        
        [Column("type")]
        public string Type { get; set; }
        
        [Column("form")]
        public string Form { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
    }

    [Table("raw_materials")]
    public class RawMaterial
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("name")]
        public string Name { get; set; }
        
        [Column("unit")]
        public string Unit { get; set; }
        
        [Column("category")]
        public string Category { get; set; }
    }

    [Table("recipes")]
    public class Recipe
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("name")]
        public string Name { get; set; }
        
        [Column("product_id")]
        public int ProductId { get; set; }
        
        [Column("version")]
        public int Version { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
    }

    [Table("production_orders")]
    public class ProductionOrder
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("order_number")]
        public string OrderNumber { get; set; }
        
        [Column("recipe_id")]
        public int RecipeId { get; set; }
        
        [Column("planned_quantity_kg")]
        public decimal PlannedQuantityKg { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
        
        [Column("planned_start_date")]
        public DateTime? PlannedStartDate { get; set; }
    }

    [Table("batches")]
    public class Batch
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("batch_number")]
        public string BatchNumber { get; set; }
        
        [Column("order_id")]
        public int OrderId { get; set; }
        
        [Column("recipe_id")]
        public int RecipeId { get; set; }
        
        [Column("tech_map_id")]
        public int TechMapId { get; set; }
        
        [Column("start_time")]
        public DateTime? StartTime { get; set; }
        
        [Column("end_time")]
        public DateTime? EndTime { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
        
        [Column("actual_quantity_kg")]
        public decimal? ActualQuantityKg { get; set; }
    }

    [Table("quality_controls")]
    public class QualityControl
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("batch_id")]
        public int? BatchId { get; set; }
        
        [Column("raw_material_batch_id")]
        public int? RawMaterialBatchId { get; set; }
        
        [Column("analysis_date")]
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
        
        [Column("sample_type")]
        public string SampleType { get; set; }
        
        [Column("parameter_name")]
        public string ParameterName { get; set; }
        
        [Column("measured_value")]
        public decimal? MeasuredValue { get; set; }
        
        [Column("standard_value")]
        public string StandardValue { get; set; }
        
        [Column("unit")]
        public string Unit { get; set; }
        
        [Column("result")]
        public string Result { get; set; }
        
        [Column("decision")]
        public string Decision { get; set; }
        
        [Column("analyst_id")]
        public int? AnalystId { get; set; }
        
        [Column("analyst_comment")]
        public string AnalystComment { get; set; }
        
        [NotMapped]
        public string Status 
        { 
            get 
            {
                if (Result == "pass" && Decision == "approved") return "✅ Одобрено";
                if (Result == "fail" && Decision == "blocked") return "❌ Забраковано";
                if (Result == "pass") return "⚠️ Требуется решение";
                if (Result == "fail") return "❌ Не соответствует";
                return "⏳ В процессе";
            }
        }
    }

    [Table("deviations")]
    public class Deviation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("batch_id")]
        public int BatchId { get; set; }
        
        [Column("deviation_type")]
        public string DeviationType { get; set; }
        
        [Column("description")]
        public string Description { get; set; }
        
        [Column("severity")]
        public string Severity { get; set; }
        
        [Column("is_resolved")]
        public bool IsResolved { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("user_id")]
        public int UserId { get; set; }
        
        [Column("message")]
        public string Message { get; set; }
        
        [Column("is_read")]
        public bool IsRead { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class DashboardStats
    {
        public int PendingBatchesCount { get; set; }
        public int CompletedBatchesCount { get; set; }
        public int TotalTestsCount { get; set; }
        public int DeviationsCount { get; set; }
    }
}