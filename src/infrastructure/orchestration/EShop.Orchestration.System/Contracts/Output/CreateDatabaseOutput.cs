using DataArc.Abstractions;
using DataArc.OrchestratR.Abstractions;

namespace EShop.Orchestration.System.Contracts.Output
{
    public class CreateDatabaseOutput : IOrchestratorOutput
    {
        public SqlResult? SqlResult { get; set; }
    }
}