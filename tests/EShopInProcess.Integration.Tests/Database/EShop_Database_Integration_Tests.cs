using DataArc;
using DataArc.OrchestratR;
using DataArc.OrchestratR.Abstractions;

using Eshop.Persistence.Models.Identity;
using EShop.Orchestration.System.Contracts.Input;
using EShop.Orchestration.System.Contracts.Output;
using EShop.Orchestration.System.Orchestrators;

using EShop.Persistence.Context.Products;
using EShop.Persistence.Contexts.Cataloging;
using EShop.Persistence.Contexts.Identity;
using EShop.Persistence.Contexts.Ordering;
using EShop.UseCases.Identity.Dtos;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework.Internal;

namespace EShopInProcess.Integration.Tests.Database
{
    public class EShop_Database_Integration_Tests
    {
        IOrchestratorHandler _orchestratorHandler;

        [SetUp]
        public void SetUp()
        {
            var configurationManager = new ConfigurationManager();
            configurationManager
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            Microsoft.Extensions.Logging.ILogger logger = factory.CreateLogger("EShopInProcess");

            var serviceProvider = new ServiceCollection()
                .AddDataArcCore(context =>
                {
                    context.AddDbContext<EShopIdentityContext>(options => options.UseSqlServer($"{configurationManager.GetConnectionString("EShopIdentity")}")
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors()
                        .UseLoggerFactory(factory));

                    context.AddDbContext<ProductsContext>(options => options.UseSqlServer($"{configurationManager.GetConnectionString("EShopProducts")}")
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors()
                       .UseLoggerFactory(factory));

                    context.AddDbContext<OrderingContext>(options => options.UseSqlServer($"{configurationManager.GetConnectionString("EShopOrdering")}")
                      .EnableSensitiveDataLogging()
                       .EnableDetailedErrors()
                       .UseLoggerFactory(factory));

                    context.AddDbContext<CatalogContext>(options => options.UseSqlServer($"{configurationManager.GetConnectionString("EShopCataloging")}")
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors()
                        .UseLoggerFactory(factory));
                })
                .AddDataArcOrchestration(
                    orc =>
                    {
                        orc.AddOrchestrator<CreateEShopDatabaseOrchestrator>();
                    }
                )
                .BuildServiceProvider();

            _orchestratorHandler = serviceProvider.GetService<IOrchestratorHandler>();
        }

        [Test]
        public void Create_ESHop_Databases_With_Identity_Generating_SQLScripts_Should_Apply_Changes_SuccessFully()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                bool generateScripts = true;
                bool applyChanges = true;

                _ = await _orchestratorHandler
                        .OrchestrateAsync<CreateEShopDatabaseOrchestrator>(
                            new CreateDatabaseInput(generateScripts, applyChanges),
                            new CreateDatabaseOutput());
            });
        }

        [Test]
        public void Seed_Eshop_Identity_Database_Should_Apply_Changes_SuccessFully()
        {
            var userDto = new UserDto
            {
                UserName = "testaccount@orchestratr.net",
                NormalizedUserName = "TESTACCOUNT@ORCHESTRATR.NET",
                NormalizedEmail = "TESTACCOUNT@ORCHESTRATR.NET",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                PasswordHash = new PasswordHasher<EShopUser>().HashPassword(null!, "Admin123!"),
                LockoutEnabled = false,
                AccessFailedCount = 0
            };

            var input = new SeedEShopIdentityDatabaseInput(userDto);
            var output = new SeedEShopIdentityDatabaseOutput();
            Assert.DoesNotThrowAsync(async () =>
            {
                output = await _orchestratorHandler
                        .OrchestrateAsync<SeedEShopIdentityDatabaseOrchestrator,
                            SeedEShopIdentityDatabaseOutput>(input, output);
            });

            Assert.That(output.UserDto.UserId, Is.GreaterThan(0));
            Assert.That(output.UserDto.UserName, Is.EqualTo(userDto.UserName));
            Assert.That(output.UserDto.NormalizedUserName, Is.EqualTo(userDto.NormalizedUserName));
            Assert.That(output.UserDto.Email, Is.EqualTo(userDto.Email));
            Assert.That(output.UserDto.NormalizedEmail, Is.EqualTo(userDto.NormalizedEmail));
            Assert.That(output.UserDto.EmailConfirmed, Is.EqualTo(userDto.EmailConfirmed));
            Assert.That(output.UserDto.SecurityStamp, Is.EqualTo(userDto.SecurityStamp));
            Assert.That(output.UserDto.PasswordHash, Is.EqualTo(userDto.PasswordHash));
            Assert.That(output.UserDto.LockoutEnabled, Is.EqualTo(userDto.LockoutEnabled));
            Assert.That(output.UserDto.AccessFailedCount, Is.EqualTo(userDto.AccessFailedCount));
        }

        /* TODO: Enable when seeding orchestrators are implemented  
         * 
        [Test]
        public async Task Seed_Eshop_Catalog_Database_Should_Apply_Changes_SuccessFully()
        {
            Assert.DoesNotThrowAsync(async () => {
                _ = await _orchestratorHandler
                        .OrchestrateAsync<SeedEShopCatalogDatabaseOrchestrator>(
                            new SeedEShopCatalogDatabaseInput(),
                            new SeedEShopCatalogDatabaseOutput());
            });
        }

        [Test]
        public async Task Seed_Eshop_Products_Database_Should_Apply_Changes_SuccessFully()
        {
            Assert.DoesNotThrowAsync(async () => {
                _ = await _orchestratorHandler
                        .OrchestrateAsync<SeedEShopProductsDatabaseOrchestrator>(
                            new SeedEShopProductsDatabaseInput(),
                            new SeedEShopProductsDatabaseOutput());
            });
        }

        [Test]
        public async Task Seed_Eshop_Ordering_Database_Should_Apply_Changes_SuccessFully()
        {
            Assert.DoesNotThrowAsync(async () => {
                _ = await _orchestratorHandler
                        .OrchestrateAsync<SeedEShopOrderingDatabaseOrchestrator>(
                            new SeedEShopOrderingDatabaseInput(),
                            new SeedEShopOrderingDatabaseOutput());
            });
        */
    }
}