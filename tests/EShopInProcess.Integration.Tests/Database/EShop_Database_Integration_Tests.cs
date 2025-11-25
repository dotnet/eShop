using DataArc;
using DataArc.OrchestratR;
using DataArc.OrchestratR.Abstractions;

using EShop.Orchestration.System.Contracts.Input;
using EShop.Orchestration.System.Contracts.Output;
using EShop.Orchestration.System.Orchestrators;

using EShop.Persistence.Contexts.Cataloging;
using EShop.Persistence.Contexts.Ordering;

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

            var sqlConnectionString = configurationManager.GetConnectionString("EShopInProcessConnectionString");

            var serviceProvider = new ServiceCollection()
                .AddDataArcCore(context =>
                {
                    context.AddDbContext<OrderingContext>(options => options.UseSqlServer($"{sqlConnectionString}")
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors()
                        .UseLoggerFactory(factory));


                    context.AddDbContext<CatalogContext>(options => options.UseSqlServer($"{sqlConnectionString}")
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
        public async Task Create_ESHop_Database_With_Identity_Using_SQLScripts_Should_Apply_Changes_SuccessFully()
        {
            bool generateScripts = true;
            bool applyChanges = true;

            var result = await _orchestratorHandler
                    .OrchestrateAsync<CreateEShopDatabaseOrchestrator, CreateDatabaseOutput>(
                        new CreateDatabaseInput(generateScripts, applyChanges),
                        new CreateDatabaseOutput());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.SqlResult, Is.Not.Null);
            Assert.That(result.SqlResult.Success, Is.True);
        }
    }
}