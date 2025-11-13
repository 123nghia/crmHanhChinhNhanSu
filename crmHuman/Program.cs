using Microsoft.AspNetCore.Authentication.Cookies;
using Quartz.Impl;
using VS.Human.Business;
using crmHuman.Services;

namespace crmHuman
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages();
            builder.Services.Config();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Home/Forbidden";
                options.LoginPath = "/Login";
            });
            builder.Services.AddHttpContextAccessor();
            
            // Đăng ký DatabaseMigrationService
            builder.Services.AddSingleton<DatabaseMigrationService>();
            
            //builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
            var app = builder.Build();
            
            // Chạy database migration khi startup
            try
            {
                var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
                await migrationService.RunMigrationsAsync();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Lỗi khi chạy database migration. Ứng dụng vẫn sẽ tiếp tục khởi động.");
            }
            
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRazorPages();
            app.Run();
            var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();
        }
    }
}