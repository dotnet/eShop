namespace Microsoft.SKEval;

public interface IInputProcessor<T>
{
    public Task<ModelOutput> Process(T userInput);
}