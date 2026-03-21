using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Quartz;
using Quartz.Impl;
using System.Globalization;
using VS.Human.Business;
using crmHuman.Services;
using crmHuman.ImpJob;

namespace crmHuman
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Force dd/MM/yyyy formatting globally via the Vietnamese culture.
            var vietnamCulture = new CultureInfo("vi-VN");
            vietnamCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            vietnamCulture.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
            vietnamCulture.DateTimeFormat.FullDateTimePattern = "dd/MM/yyyy HH:mm:ss";
            CultureInfo.DefaultThreadCurrentCulture = vietnamCulture;
            CultureInfo.DefaultThreadCurrentUICulture = vietnamCulture;
            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(vietnamCulture),
                SupportedCultures = new[] { vietnamCulture },
                SupportedUICultures = new[] { vietnamCulture },
                ApplyCurrentCultureToResponseHeaders = true
            };
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
            builder.Services.AddHostedService<AttendanceRealtimeSyncService>();
            builder.Services.AddHostedService<StartupMailTestService>();
            
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
                if (app.Environment.IsDevelopment())
                {
                    throw;
                }
            }
            
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseRequestLocalization(localizationOptions);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<AuditLogMiddleware>();
            app.MapRazorPages();

            // Khoi chay Quartz scheduler
            try 
            {
                var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
                await scheduler.Start();

                ContractExpiryJob.ServiceProvider = app.Services;
                var jobKey = new JobKey("ContractExpiryJob");
                if (!await scheduler.CheckExists(jobKey))
                {
                    var job = JobBuilder.Create<ContractExpiryJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("Days", 30)
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity("ContractExpiryTrigger")
                        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(8, 0))
                        .Build();

                    await scheduler.ScheduleJob(job, trigger);
                }
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Loi khi khoi chay Quartz scheduler.");
            }

            app.Run();
        }
    }
}
