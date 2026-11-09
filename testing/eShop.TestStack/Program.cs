using eShop.TestStack;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    return await Commands.RunAsync(ParsedArgs.Parse(args), cts.Token);
}
catch (TestStackException ex)
{
    Output.Log($"error: {ex.Message}");
    Output.Result(new System.Text.Json.Nodes.JsonObject { ["ok"] = false, ["exitCode"] = ex.ExitCode, ["error"] = ex.Message });
    return ex.ExitCode;
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Output.Log("cancelled");
    return ExitCodes.Error;
}
catch (Exception ex)
{
    Output.Log($"error: {ex}");
    Output.Result(new System.Text.Json.Nodes.JsonObject { ["ok"] = false, ["exitCode"] = ExitCodes.Error, ["error"] = ex.GetBaseException().Message });
    return ExitCodes.Error;
}
