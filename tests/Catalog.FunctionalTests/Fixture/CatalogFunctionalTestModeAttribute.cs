using System.Reflection;

using Xunit.v3;

namespace eShop.Catalog.FunctionalTests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CatalogFunctionalTestModeAttribute : BeforeAfterTestAttribute, ITraitAttribute
{
    public CatalogFunctionalTestModeAttribute(CatalogFunctionalTestMode mode)
    {
        Mode = mode;
    }

    public CatalogFunctionalTestMode Mode { get; }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
    [
        new(FunctionalTestModeTrait.Name, FunctionalTestModeTrait.ToTraitValue(Mode))
    ];

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var attributedMode = Resolve(methodUnderTest.DeclaringType!, methodUnderTest);
        var overrideMode = FunctionalTestModeReader.ReadOverrideFromEnvironment();

        if (overrideMode is not null && overrideMode != attributedMode)
        {
            throw new Exception(
                $"$XunitDynamicSkip$Skipped because test requires '{FunctionalTestModeTrait.ToTraitValue(attributedMode)}' " +
                $"but {FunctionalTestModeReader.EnvironmentVariableName} is set to '{FunctionalTestModeTrait.ToTraitValue(overrideMode.Value)}'.");
        }
    }

    public static CatalogFunctionalTestMode Resolve(Type testClass, MethodInfo method)
    {
        var methodAttribute = method.GetCustomAttribute<CatalogFunctionalTestModeAttribute>(inherit: true);
        if (methodAttribute is not null)
        {
            return methodAttribute.Mode;
        }

        var classAttribute = testClass.GetCustomAttribute<CatalogFunctionalTestModeAttribute>(inherit: true);
        if (classAttribute is not null)
        {
            return classAttribute.Mode;
        }

        return CatalogFunctionalTestMode.Aspire;
    }

    public static CatalogFunctionalTestMode Resolve(Type testClass, string methodName, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
    {
        var method = testClass.GetMethod(methodName, bindingFlags)
            ?? throw new InvalidOperationException($"Could not find test method '{testClass.FullName}.{methodName}'.");

        return Resolve(testClass, method);
    }
}
