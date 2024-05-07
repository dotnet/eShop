namespace Microsoft.SKEval;

public interface IOutputProcessor
{
    public Task Init();

    public Task Process(BatchEvalPromptOutput evalOutput);
}