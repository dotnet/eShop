using DataArc.Abstractions;
using DataArc.OrchestratR;

using EShop.Orchestration.System.Contracts.Input;
using EShop.Orchestration.System.Contracts.Output;

using EShop.Persistence.Context.Products;
using EShop.Persistence.Contexts.Cataloging;
using EShop.Persistence.Contexts.Identity;
using EShop.Persistence.Contexts.Ordering;

namespace EShop.Orchestration.System.Orchestrators
{
    public class CreateEShopDatabaseOrchestrator(IDatabaseDefinitionBuilder databaseDefinitionBuilder) 
        : Orchestrator<CreateDatabaseInput, CreateDatabaseOutput>
    {
        public override async Task<CreateDatabaseOutput> ExecuteAsync(CreateDatabaseInput input, CreateDatabaseOutput output)
        {
            try
            {
                var database = databaseDefinitionBuilder
                                .UseContext<EShopIdentityContext>()
                                .UseContext<ProductsContext>()
                                .UseContext<OrderingContext>()
                                .UseContext<CatalogContext>()
                                .Build(generateScripts: input.GenerateScripts, applyChanges: input.ApplyChanges);

                database.ExecuteDrop();
                database.ExecuteCreate();
            }
            catch
            {
                throw;
            }

            await Task.Yield();
            return output;
        }
    }
}