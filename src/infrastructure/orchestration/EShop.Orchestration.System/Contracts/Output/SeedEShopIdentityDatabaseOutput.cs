using DataArc.OrchestratR.Abstractions;
using EShop.UseCases.Identity.Dtos;

namespace EShop.Orchestration.System.Contracts.Output
{
    public class SeedEShopIdentityDatabaseOutput : IOrchestratorOutput
    {
        public UserDto UserDto { get; set; }
    }
}