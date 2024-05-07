using System.Security.Claims;
using eShop.WebApp.Chatbot;
using eShop.WebAppComponents.Services;
using eShop.WebApp.Services;
using WebApp.UnitTests;
using Xunit.Abstractions;
using Microsoft.SKEval;
using eShop.WebAppComponents.Catalog;
using NSubstitute;

namespace eShop.WebApp.UnitTests;

public class ChatStateTests : IClassFixture<PromptEvaluationFixture>
{
    private PromptEvaluationFixture _promptEvaluator = default!;

    private readonly ITestOutputHelper _output;

    public ChatStateTests(PromptEvaluationFixture fixture, ITestOutputHelper output)
    {
        _promptEvaluator = fixture;
        _output = output;
    }

    [Theory]
    [InlineData("Do you have any backpack in the catalog?", 3)]
  //[InlineData("In the Northern Mountains catalog I saw a book about python, but the image is blurry. Could you send me the code to connecting to azure blob storage that is in the book first chapter?", 1)]
    [InlineData("Add the best reviewed gift for a kid less than $20 usd", 3)]
    [InlineData("What are the top brands in your catalog?", 3)]
    public async Task AskingCommonQuestionsReturnCoherentResults(string question, int minRelevance)
    {
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
        var mockCatalogService = Substitute.For<ICatalogService>();
        var basketState = Substitute.For<IBasketState>();
        ClaimsPrincipal user = default!;
        var productImages = Substitute.For<IProductImageUrlProvider>();

        var kidsItems = new List<CatalogItem>()
        {
            new(1, "Kids Mountain Bike", "Mountain Bike 13'", 140, string.Empty, 1, null, 1, null),
            new(1, "Kids Balaclava", "Youth Balaclava black", 15, string.Empty, 1, null, 1, null)
        };
        mockCatalogService.GetCatalogItemsWithSemanticRelevance(Arg.Any<int>(), Arg.Any<int>(), Arg.Is<string>(x => x.Contains("kid"))).Returns(Task.FromResult(new CatalogResult(0, 8, 8, kidsItems)));

        var backpackItems = new List<CatalogItem>()
        {
            new(1, "Alpine Backpack", "Ski Backpack Raccoon Style", 35, string.Empty, 1, null, 1, null)
        };
        mockCatalogService.GetCatalogItemsWithSemanticRelevance(Arg.Any<int>(), Arg.Any<int>(), "backpack").Returns(Task.FromResult(new CatalogResult(0, 8, 8, backpackItems)));

        var brands = new List<CatalogBrand>()
        {
            new(1, "Alpine"),
            new(2, "Raccoon"),
            new(3, "PNW"),
            new(1, "Rocket")
        };

        mockCatalogService.GetBrands().Returns(brands);

        var chatState = new ChatState(mockCatalogService, basketState, user, productImages, _promptEvaluator.Kernel, mockLoggerFactory);

        await chatState.AddUserMessageAsync(question, () => { /* message added, do we want to do something with this event? */ });
            
        var modelOutput = new ModelOutput()
        {
            Input = question,
            Output = chatState.Messages.LastOrDefault()?.ToString()
        };

        var score = await RelevanceEval.GetInstance(_promptEvaluator.Kernel).Eval(modelOutput);

        _output.WriteLine($"Q: {modelOutput.Input}");
        _output.WriteLine($"A: {modelOutput.Output}.");
        _output.WriteLine($"Relevance: {score}.");

        Assert.True(score >= minRelevance, $"Relevance of {question} - score {score}, expecting min {minRelevance}.");
    }
}
