using DataArc.Abstractions;
using DataArc.OrchestratR;

using Eshop.Persistence.Models.Identity;
using EShop.Orchestration.System.Contracts.Input;
using EShop.Orchestration.System.Contracts.Output;
using EShop.Persistence.Contexts.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace EShop.Orchestration.System.Orchestrators
{
    /// <summary>
    /// Infrastructure-level orchestrator responsible for coordinating all
    /// database operations required to seed the EShop Identity database.
    /// 
    /// This orchestrator contains:
    ///  • No business logic
    ///  • No domain rules
    ///  • No request/response handlers
    ///  • No service-layer dependencies
    ///
    /// It demonstrates DataArc’s core architecture:
    ///  • True CQRS separation using dedicated command and query pipelines
    ///  • Multi-context orchestration (single or multi-database) without migrations
    ///  • Deterministic, transactional consistency without event buses or messaging
    ///  • Infrastructure coordination only — orchestrators remain intentionally "dumb"
    ///
    /// All seeding is executed through DataArc’s asynchronous database command
    /// and query builders, ensuring predictable, EF Core–safe, multi-entity batches
    /// with immediate consistency. Exceptions are intentionally surfaced to the caller,
    /// keeping failure handling at the execution boundary rather than in the orchestrator.
    /// </summary>
    public class SeedEShopIdentityDatabaseOrchestrator : Orchestrator<SeedEShopIdentityDatabaseInput, SeedEShopIdentityDatabaseOutput>
    {
        private readonly IAsyncDatabaseCommandBuilder _asyncDatabaseCommandBuilder;
        private readonly IAsyncDatabaseQueryBuilder _asyncDatabaseQueryBuilder;
        private readonly ILogger<SeedEShopIdentityDatabaseOrchestrator> _logger;

        public SeedEShopIdentityDatabaseOrchestrator(
            IAsyncDatabaseCommandBuilder asyncDatabaseCommandBuilder,
            IAsyncDatabaseQueryBuilder asyncDatabaseQueryBuilder,
            ILogger<SeedEShopIdentityDatabaseOrchestrator> logger
            )
        {
            _asyncDatabaseCommandBuilder = asyncDatabaseCommandBuilder;
            _asyncDatabaseQueryBuilder = asyncDatabaseQueryBuilder;
            _logger = logger;
        }

        public override async Task<SeedEShopIdentityDatabaseOutput> ExecuteAsync(SeedEShopIdentityDatabaseInput input, SeedEShopIdentityDatabaseOutput output)
        {
            try
            {
                //Input defined user data
                var user = new EShopUser
                {
                    UserName = input.UserDto.UserName,
                    NormalizedUserName = input.UserDto.NormalizedUserName,
                    Email = input.UserDto.NormalizedUserName,
                    NormalizedEmail = input.UserDto.NormalizedUserName,
                    EmailConfirmed = input.UserDto.EmailConfirmed,
                    SecurityStamp = input.UserDto.SecurityStamp,
                    PasswordHash = input.UserDto.PasswordHash,
                    LockoutEnabled = input.UserDto.LockoutEnabled,
                    AccessFailedCount = input.UserDto.AccessFailedCount
                };

                //Explicitly defined user claims
                var identityUserClaims = new List<IdentityUserClaim<int>>
                {
                    new IdentityUserClaim<int> { UserId = 1, ClaimType = "sub", ClaimValue = "1" },
                    new IdentityUserClaim<int> { UserId = 1, ClaimType = "email", ClaimValue = input.UserDto.NormalizedEmail },
                    new IdentityUserClaim<int> { UserId = 1, ClaimType = "name", ClaimValue = input.UserDto.UserName },
                    new IdentityUserClaim<int> { UserId = 1, ClaimType = "role", ClaimValue = "SuperAdmin" }
                };

                var seedIdentityDatabaseCommand = await _asyncDatabaseCommandBuilder
                    .UseCommandContext<EShopIdentityContext>()
                    .Add(new IdentityRole<int> { Name = "SuperAdmin", NormalizedName = "SUPERADMIN" }) //explicitly add role
                    .Add(user)
                    .Add(new IdentityUserRole<int>
                    {
                        UserId = 1,
                        RoleId = 1
                    })// DataArc allows explicit key insertion
                    .AddRange(identityUserClaims)
                    .BuildAsync();

                var result = await seedIdentityDatabaseCommand.ExecuteAsync();
                if(!result.Success) //DataArc returns a SQLResult indicating success or failure including error messages
                {
                    _logger.LogError("Seeding EShop Identity Database failed: {Errors}", string.Join(", ", result.Errors));
                }

                //Retrieve the created user to return in output
                var createdUser = await _asyncDatabaseQueryBuilder
                    .UseQueryContext<EShopIdentityContext>()
                    .ReadOneAsync<EShopUser>(input.UserDto.UserId);

                if(createdUser == null)
                    return output; //Return empty output if user creation failed

                output = new SeedEShopIdentityDatabaseOutput
                {
                    UserDto = new UseCases.Identity.Dtos.UserDto
                    {
                        UserId = createdUser.Id,
                        UserName = createdUser.UserName!,
                        NormalizedUserName = createdUser.NormalizedUserName!,
                        NormalizedEmail = createdUser.NormalizedEmail!,
                        EmailConfirmed = createdUser.EmailConfirmed,
                        SecurityStamp = createdUser.SecurityStamp!,
                        PasswordHash = createdUser.PasswordHash!,
                        LockoutEnabled = createdUser.LockoutEnabled,
                        AccessFailedCount = createdUser.AccessFailedCount
                    }
                };

                return output;
            }
            catch
            {
                throw; //Throw exception up the call stack
            }
        }
    }
}