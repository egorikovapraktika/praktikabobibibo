using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperatorModule.Models
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

    [Table("tech_maps")]
    public class TechMap
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("product_id")]
        public int ProductId { get; set; }
        
        [Column("version")]
        public int Version { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
    }

    [Table("tech_map_steps")]
    public class TechMapStep
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("tech_map_id")]
        public int TechMapId { get; set; }
        
        [Column("step_order")]
        public int StepOrder { get; set; }
        
        [Column("step_name")]
        public string StepName { get; set; }
        
        [Column("step_type")]
        public string StepType { get; set; }
        
        [Column("planned_temp_c")]
        public decimal? PlannedTempC { get; set; }
        
        [Column("planned_pressure_bar")]
        public decimal? PlannedPressureBar { get; set; }
        
        [Column("planned_duration_min")]
        public int? PlannedDurationMin { get; set; }
        
        [Column("is_mandatory")]
        public bool IsMandatory { get; set; } = true;
        
        [Column("instruction")]
        public string Instruction { get; set; }
    }

    [Table("batch_steps")]
    public class BatchStep
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("batch_id")]
        public int BatchId { get; set; }
        
        [Column("step_order")]
        public int StepOrder { get; set; }
        
        [Column("step_name")]
        public string StepName { get; set; }
        
        [Column("actual_temp_c")]
        public decimal? ActualTempC { get; set; }
        
        [Column("actual_pressure_bar")]
        public decimal? ActualPressureBar { get; set; }
        
        [Column("actual_duration_min")]
        public int? ActualDurationMin { get; set; }
        
        [Column("started_by")]
        public int? StartedBy { get; set; }
        
        [Column("completed_by")]
        public int? CompletedBy { get; set; }
        
        [Column("started_at")]
        public DateTime? StartedAt { get; set; }
        
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
        
        [Column("deviation_flag")]
        public bool DeviationFlag { get; set; }
        
        [Column("operator_comment")]
        public string OperatorComment { get; set; }
    }

    [Table("deviations")]
    public class Deviation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("batch_id")]
        public int BatchId { get; set; }
        
        [Column("description")]
        public string Description { get; set; }
        
        [Column("severity")]
        public string Severity { get; set; }
        
        [Column("is_resolved")]
        public bool IsResolved { get; set; }
        
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
    }
}