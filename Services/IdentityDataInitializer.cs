using Microsoft.AspNetCore.Identity;



public static class IdentityDataInitializer
{
    public const string ManagerRole = "Manager";
    public const string DefaultAdminEmail = "manager@logitrack.com";
    // Th1s1s@5trongPa55w0rdF0rTh3Adm!n

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(ManagerRole))
        {
            await roleManager.CreateAsync(new IdentityRole(ManagerRole));
        }

        var adminUser = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (adminUser == null)
        {
            string adminPassword = Environment.GetEnvironmentVariable("LOGITRACK_DEFAULT_ADMIN_PASSWORD")
                ?? throw new InvalidOperationException(
                    "DEFAULT_ADMIN_PASSWORD environment variable is required for initial admin seeding. " +
                    "Please set it before starting the application.");

            adminUser = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, ManagerRole);
                Console.WriteLine("Default Manager user created.");
            }
            else
            {
                throw new Exception("Unable to create a default Manager user: " + 
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}