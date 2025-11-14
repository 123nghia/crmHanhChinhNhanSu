using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace Human.CDN2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Force every DateTime render/bind to use dd/MM/yyyy by default.
            var vietnamCulture = new CultureInfo("vi-VN");
            vietnamCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            vietnamCulture.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
            CultureInfo.DefaultThreadCurrentCulture = vietnamCulture;
            CultureInfo.DefaultThreadCurrentUICulture = vietnamCulture;
            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(vietnamCulture),
                SupportedCultures = new[] { vietnamCulture },
                SupportedUICultures = new[] { vietnamCulture },
                ApplyCurrentCultureToResponseHeaders = true
            };

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRequestLocalization(localizationOptions);
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
