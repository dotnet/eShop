using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShop.Testing.Common;

public abstract class MSTestLoggingTestBase
{
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void InitializeTestLogging()
    {
        TestLogging.ConfigureMSTestOutput(TestContext);
    }

    [TestCleanup]
    public void FlushTestLogging()
    {
        TestLogging.FlushToTestOutput();
        TestOutputWriter.SetWriter(null);
    }
}
