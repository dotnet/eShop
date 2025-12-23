using Microsoft.AspNetCore.Identity;

namespace eShop.Identity.API;

public class UsersSeed(
    ILogger<UsersSeed> logger,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IDbSeeder<ApplicationDbContext>
{
    public async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed Admin role
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogDebug("Admin role created");
        }

        // Seed Shipper role
        if (!await roleManager.RoleExistsAsync("Shipper"))
        {
            await roleManager.CreateAsync(new IdentityRole("Shipper"));
            logger.LogDebug("Shipper role created");
        }

        // Seed admin user
        var admin = await userManager.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@eshop.com",
                EmailConfirmed = true,
                CardHolderName = "Admin User",
                CardNumber = "XXXXXXXXXXXX0000",
                CardType = 1,
                City = "Seattle",
                Country = "U.S.",
                Expiration = "12/25",
                Id = Guid.NewGuid().ToString(),
                LastName = "Admin",
                Name = "System",
                PhoneNumber = "0000000000",
                ZipCode = "98101",
                State = "WA",
                Street = "1 Admin Way",
                SecurityNumber = "000"
            };

            var result = await userManager.CreateAsync(admin, "Admin123$");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogDebug("admin user created and assigned to Admin role");
            }
            else
            {
                throw new Exception(result.Errors.First().Description);
            }
        }
        else
        {
            // Ensure existing admin has Admin role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogDebug("Admin role assigned to existing admin user");
            }
        }

        // Seed shipper user
        var shipper = await userManager.FindByNameAsync("shipper");
        if (shipper == null)
        {
            shipper = new ApplicationUser
            {
                UserName = "shipper",
                Email = "shipper@eshop.com",
                EmailConfirmed = true,
                CardHolderName = "Default Shipper",
                CardNumber = "XXXXXXXXXXXX0000",
                CardType = 1,
                City = "Seattle",
                Country = "U.S.",
                Expiration = "12/25",
                Id = Guid.NewGuid().ToString(),
                LastName = "Shipper",
                Name = "Default",
                PhoneNumber = "1234567890",
                ZipCode = "98101",
                State = "WA",
                Street = "1 Shipper Way",
                SecurityNumber = "000"
            };

            var result = await userManager.CreateAsync(shipper, "Shipper123$");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(shipper, "Shipper");
                logger.LogDebug("shipper user created and assigned to Shipper role");
            }
            else
            {
                throw new Exception(result.Errors.First().Description);
            }
        }
        else
        {
            // Ensure existing shipper has Shipper role
            if (!await userManager.IsInRoleAsync(shipper, "Shipper"))
            {
                await userManager.AddToRoleAsync(shipper, "Shipper");
                logger.LogDebug("Shipper role assigned to existing shipper user");
            }
        }

        var alice = await userManager.FindByNameAsync("alice");

        if (alice == null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Alice Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Alice",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "123"
            };

            var result = await userManager.CreateAsync(alice, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice already exists");
            }
        }

        var bob = await userManager.FindByNameAsync("bob");

        if (bob == null)
        {
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Bob Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Bob",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "456"
            };

            var result = await userManager.CreateAsync(bob, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob already exists");
            }
        }
    }
}
