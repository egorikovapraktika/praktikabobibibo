using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProductionAPI
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(@"Server=KAB17-07\SQLEXPRESS;Database=dbpraktikchka;Trusted_Connection=True;"));

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("AllowAll");
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("API работает!");
                });

                endpoints.MapGet("/api/products", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var products = await db.Products.ToListAsync();
                    var json = JsonSerializer.Serialize(products);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapPost("/api/auth/login", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<LoginRequest>(body);
                    
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == data.Email);
                    
                    var response = new
                    {
                        access_token = $"token-{Guid.NewGuid()}",
                        user = new
                        {
                            Id = user?.Id ?? 1,
                            Email = data.Email,
                            FullName = user?.FullName ?? "Пользователь",
                            Role = "technologist"
                        }
                    };
                    
                    var json = JsonSerializer.Serialize(response);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapPost("/api/auth/register", async context =>
                {
                    var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<RegisterRequest>(body);
                    
                    var response = new { Id = 1, Email = data.Email, Message = "Регистрация успешна" };
                    var json = JsonSerializer.Serialize(response);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapGet("/api/recipes", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var recipes = await db.Recipes.ToListAsync();
                    var json = JsonSerializer.Serialize(recipes);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapGet("/api/tech-maps", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var techMaps = await db.TechMaps.ToListAsync();
                    var json = JsonSerializer.Serialize(techMaps);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapGet("/api/production-orders", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var orders = await db.ProductionOrders.ToListAsync();
                    var json = JsonSerializer.Serialize(orders);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapGet("/api/batches", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var batches = await db.Batches.ToListAsync();
                    var json = JsonSerializer.Serialize(batches);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapGet("/api/deviations", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var deviations = await db.Deviations.ToListAsync();
                    var json = JsonSerializer.Serialize(deviations);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });

                endpoints.MapGet("/api/notifications", async context =>
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var notifications = await db.Notifications.ToListAsync();
                    var json = JsonSerializer.Serialize(notifications);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });
            });
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:5000");
                });
    }

    // МОДЕЛИ
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<TechMap> TechMaps { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<User> Users { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Form { get; set; }
        public string Status { get; set; }
    }

    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductId { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
    }

    public class TechMap
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductId { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
    }

    public class ProductionOrder
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int RecipeId { get; set; }
        public string Status { get; set; }
    }

    public class Batch
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; }
    }

    public class Deviation
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public string Description { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }
}