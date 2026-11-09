using System.Text.Json.Nodes;

namespace eShop.TestStack.Tests;

[TestClass]
public sealed class ScenarioParserTests
{
    private static readonly string[] Databases = ["catalogdb", "orderingdb"];
    private string _dir = null!;

    [TestInitialize]
    public void Setup()
    {
        _dir = Directory.CreateTempSubdirectory("teststack-").FullName;
        Write("_base/scenario.yaml", "postgres:\n  orderingdb:\n    - table: ordering.cardtypes\n      rows: [{ Id: 1, Name: Amex }]\n");
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_dir, recursive: true);

    [TestMethod]
    public void ResolveOrdersBaseThenExtendsThenScenarioWithoutDuplicates()
    {
        Write("a/scenario.yaml", "extends: _base\n");
        Write("b/scenario.yaml", "extends: [a, _base]\n");

        var layers = ScenarioParser.Resolve("b", _dir, "_base", Databases);

        CollectionAssert.AreEqual(new[] { "_base", "a", "b" }, layers.Select(l => l.Name).ToArray());
    }

    [TestMethod]
    public void ResolveRejectsExtendsCycles()
    {
        Write("a/scenario.yaml", "extends: b\n");
        Write("b/scenario.yaml", "extends: a\n");

        Assert.ThrowsExactly<TestStackException>(() => ScenarioParser.Resolve("a", _dir, "_base", Databases));
    }

    [TestMethod]
    public void YamlScalarsKeepLeadingZeroStringsAndTypeNumbersBooleansAndNulls()
    {
        Write("s/scenario.yaml", "postgres:\n  catalogdb:\n    - table: Catalog\n      rows: [{ Zip: 02134, Price: 9.5, Id: 7, OnReorder: false, Description: null, Quoted: \"42\" }]\n");

        var layer = ScenarioParser.ParseFile(Path.Combine(_dir, "s/scenario.yaml"), Databases);
        var row = ((TableStep)layer.Postgres.Single(p => p.Database == "catalogdb").Steps.Single()).Rows.Single()!.AsObject();

        Assert.AreEqual("02134", row["Zip"]!.GetValue<string>());
        Assert.AreEqual(9.5m, row["Price"]!.GetValue<decimal>());
        Assert.AreEqual(7, row["Id"]!.GetValue<int>());
        Assert.IsFalse(row["OnReorder"]!.GetValue<bool>());
        Assert.IsNull(row["Description"]);
        Assert.AreEqual("42", row["Quoted"]!.GetValue<string>());
    }

    [TestMethod]
    [DataRow("unknown: 1\n", DisplayName = "unknown top-level key")]
    [DataRow("postgres:\n  nosuchdb:\n    - sql: x.sql\n", DisplayName = "unknown database")]
    [DataRow("postgres:\n  catalogdb:\n    - table: Catalog\n", DisplayName = "table step without rows")]
    [DataRow("postgres:\n  catalogdb:\n    - sql: missing.sql\n", DisplayName = "missing sql file")]
    public void ParseFileRejectsInvalidScenarios(string yaml)
    {
        Write("bad/scenario.yaml", yaml);

        Assert.ThrowsExactly<TestStackException>(() => ScenarioParser.ParseFile(Path.Combine(_dir, "bad/scenario.yaml"), Databases));
    }

    [TestMethod]
    public void RedisJsonValuesAreSerializedCompactly()
    {
        Write("r/scenario.yaml", "redis:\n  - key: /basket/u1\n    json: { BuyerId: u1, Items: [] }\n    ttl: 60\n");

        var entry = ScenarioParser.ParseFile(Path.Combine(_dir, "r/scenario.yaml"), Databases).Redis.Single();

        Assert.AreEqual("/basket/u1", entry.Key);
        Assert.AreEqual("u1", JsonNode.Parse(entry.Value)!["BuyerId"]!.GetValue<string>());
        Assert.AreEqual(TimeSpan.FromSeconds(60), entry.Ttl);
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
