namespace eShop.TestStack.Tests;

[TestClass]
public sealed class PasswordsTests
{
    [TestMethod]
    public void HashVerifiesWithAspNetIdentityHasher()
    {
        var hash = Passwords.Hash("Pass123$");

        Assert.IsTrue(Passwords.Verify(hash, "Pass123$"));
        Assert.IsFalse(Passwords.Verify(hash, "wrong"));
    }

    [TestMethod]
    public void CommittedScenarioHashesMatchDocumentedPassword()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "testing", "teststack.json")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Repository root not found.");
        }

        var layer = ScenarioParser.ParseFile(Path.Combine(root, "testing", "scenarios", "default", "scenario.yaml"), ["catalogdb", "identitydb"]);
        var users = ((TableStep)layer.Postgres.Single(p => p.Database == "identitydb").Steps.Single()).Rows;

        Assert.AreEqual(2, users.Count);
        foreach (var user in users)
        {
            Assert.IsTrue(Passwords.Verify(user!["PasswordHash"]!.GetValue<string>(), "Pass123$"), user!["UserName"]!.ToString());
        }
    }
}
