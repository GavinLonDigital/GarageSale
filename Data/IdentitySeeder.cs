using Microsoft.AspNetCore.Identity;

namespace GarageSale.Data;

public class IdentitySeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db,
    ILogger<IdentitySeeder> logger)
{
    private const string DefaultPassword = "Temp123$!";

    private static readonly string[] Roles = ["Admin", "Seller", "User"];

    private static readonly SeedUser[] Users =
    [
        new("wendydaniels@hotmail.com",   "Wendy",   "1284 Maple Ridge Dr",   "Carmel", "IN", "46032", @"images/Hero/wendy_logo.png",   "Seller"),
        new("johnbale@hotmail.com",       "John",    "613 Oak Hollow Ct",     "Carmel", "IN", "46032", @"images/Hero/john_logo.png",    "Seller"),
        new("bobjones@hotmail.com",       "Bob",     "947 Willow Creek Ln",   "Carmel", "IN", "46032", @"images/Hero/bob_logo.png",     "Seller"),
        new("michaelthompson@hotmail.com","Michael", "1528 Brookstone Way",   "Carmel", "IN", "46032", @"images/michael_logo.png",      "Admin", "Admin123$!"),
        new("kevinanderson@hotmail.com",  "Kevin",   "11109 Stonebridge Way", "Carmel", "IN", "46032", @"images/kevin_logo.png",        "User"),
    ];

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (result.Succeeded)
                logger.LogInformation("Created role '{Role}'", role);
            else
                throw new InvalidOperationException(
                    $"Failed to create role '{role}': {FormatErrors(result)}");
        }
    }

    private async Task SeedUsersAsync()
    {
        foreach (var data in Users)
        {
            var user = await userManager.FindByEmailAsync(data.Email);

            if (user is not null)
            {
                await EnsureInRoleAsync(user, data.Email, data.Role);
                continue;
            }

            user = new ApplicationUser
            {
                UserName       = data.Email,
                Email          = data.Email,
                EmailConfirmed = true,
                FirstName      = data.FirstName,
                Address1       = data.Address1,
                Address2       = data.Address2,
                Address3       = data.Address3,
                ZipOrPostCode  = data.ZipOrPostCode,
            };

            var createResult = await userManager.CreateAsync(user, data.Password ?? DefaultPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create user '{data.Email}': {FormatErrors(createResult)}");

            var roleResult = await userManager.AddToRoleAsync(user, data.Role);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to assign role '{data.Role}' to '{data.Email}': {FormatErrors(roleResult)}");

            if (data.Role == "Seller")
            {
                db.SellerDetails.Add(new SellerDetails
                {
                    UserId             = user.Id,
                    SellerBusinessName = $"{data.FirstName}'s Garage Sale",
                    LogoPath           = data.LogoPath,
                });
                await db.SaveChangesAsync();
            }

            logger.LogInformation("Seeded user '{Email}' with role '{Role}'", data.Email, data.Role);
        }
    }

    private async Task EnsureInRoleAsync(ApplicationUser user, string email, string role)
    {
        if (await userManager.IsInRoleAsync(user, role))
        {
            logger.LogDebug("User '{Email}' already exists and is already in role '{Role}' — skipping", email, role);
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to assign role '{role}' to existing user '{email}': {FormatErrors(roleResult)}");

        logger.LogInformation("Added existing user '{Email}' to role '{Role}'", email, role);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));

    private sealed record SeedUser(
        string Email,
        string FirstName,
        string Address1,
        string Address2,
        string Address3,
        string ZipOrPostCode,
        string LogoPath,
        string Role,
        string? Password = null);
}
