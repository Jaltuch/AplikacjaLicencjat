using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;

namespace TableTenisWebApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            
            var builder = WebApplication.CreateBuilder(args);
            

            // Rejestracja bazy danych

            builder.Services.AddDbContext<AppIdentityDbContext>(opt =>
                opt.UseSqlite
                (builder.Configuration.GetConnectionString("DefaultConnection")));
            // ── Identity + Role
            builder.Services
                .AddDefaultIdentity<ApplicationUser>(opt => opt.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>();

           
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

          

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                
                app.UseHsts();
            }
            app.UseHsts(); // Produkcyjne – wymusza HTTPS w przeglądarce

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always
            });


            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            await IdentitySeeder.SeedAsync(app.Services);

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            
            app.MapRazorPages();
            app.Run();
        }
    }
}
