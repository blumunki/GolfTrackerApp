// In GolfTrackerApp.Web/Data/SeedData.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // For EnsureCreated/Migrate
using Microsoft.Extensions.DependencyInjection;

namespace GolfTrackerApp.Web.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Create the roles and seed them to the database
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Optionally create a default Admin user
            //var userManager = services.GetRequiredService<UserManager<IdentityUser>>(); // Or ApplicationUser
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>(); // Use ApplicationUser
            string adminEmail = "admin@golftracker.local"; // Change as needed
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                //adminUser = new IdentityUser // Or ApplicationUser
                adminUser = new ApplicationUser // Use ApplicationUser here too
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Assuming you want admin pre-confirmed
                };
                // IMPORTANT: Set a strong password. For real apps, get this from config/secrets.
                var result = await userManager.CreateAsync(adminUser, "AdminPa$$w0rd!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}