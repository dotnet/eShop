using System.Reflection;
using eShop.Testing.Common;
using Xunit.v3;

namespace eShop.Catalog.FunctionalTests;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FlushTestLogsAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        TestLogging.ClearCapturedLogs();
        TestOutputWriter.SetWriter(null);
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        TestLogging.FlushToTestOutput();
        TestOutputWriter.SetWriter(null);
    }
}
