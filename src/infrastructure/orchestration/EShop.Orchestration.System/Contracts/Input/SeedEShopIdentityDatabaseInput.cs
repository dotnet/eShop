using DataArc.OrchestratR.Abstractions;

using EShop.UseCases.Identity.Dtos;

namespace EShop.Orchestration.System.Contracts.Input
{
    public record SeedEShopIdentityDatabaseInput(UserDto UserDto) : IOrchestratorInput;
}