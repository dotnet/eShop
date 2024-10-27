
namespace eShop.Identity.API;

public class UsersSeed(ILogger<UsersSeed> logger, 
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
    ) : IDbSeeder<ApplicationDbContext>
{
    public async Task SeedAsync(ApplicationDbContext context)
    {
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

            var result = userManager.CreateAsync(alice, "Pass123$").Result;

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


       
        var exists = await roleManager.RoleExistsAsync("Administrator");
        if (!exists)
        {
            var admin = new IdentityRole
            {
                Name = "Administrator"
            };
            await roleManager.CreateAsync(admin);
            logger.LogInformation("Created Administrator role");
        }

        var adminRole = await roleManager.FindByNameAsync("Administrator");
        //var userMgr = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("alice");

        if (user != null)
        {

            var isAdmin = await userManager.IsInRoleAsync(user, "Administrator");
            if (!isAdmin)
            {
                await userManager.AddToRoleAsync(user, "Administrator");
                logger.LogDebug("alice added to Administator role");
            } else
            {
                logger.LogDebug("alice already  have Administrator role");
            }

            //var claims = await userMgr.GetClaimsAsync(user);
            //if (!claims.Any(c => c.Type == "location"))
            //{
            //    var locationClaim = new Claim("location", "Philadelphia");
            //    await userMgr.AddClaimAsync(user, locationClaim);
            //}

        }
    }
}

