using DataArc.OrchestratR.Abstractions;

namespace EShop.Orchestration.System.Contracts.Input
{
    public record CreateDatabaseInput(bool GenerateScripts, bool ApplyChanges) : IOrchestratorInput;
}