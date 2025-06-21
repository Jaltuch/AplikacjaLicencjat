using Microsoft.AspNetCore.Identity;
using TableTenisWebApp.Models;

namespace TableTenisWebApp.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // ① Role: Admin, Organizer, Player
            string[] roles = { "Admin", "Organizer", "Player" };
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            // ② Konto admin
            const string adminMail = "admin@pingpong.local";
            var admin = await userMgr.FindByEmailAsync(adminMail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminMail,
                    Email = adminMail
                };
                await userMgr.CreateAsync(admin, "Admin123!");
                await userMgr.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
