namespace Microsoft.SKEval;

public interface IOutputProcessor
{
    public Task Init();

    public void Process(BatchEvalPromptOutput evalOutput);
}
