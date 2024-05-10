using System.Security.Claims;
using eShop.WebApp.Chatbot;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SKEval;
using NSubstitute;
using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;
using eShop.WebApp.Services;

namespace AI.BatchEvals
{
    internal class ChatInputProcessor : IInputProcessor<ModelInputQA>
    {
        private readonly ChatState chatState;

        public ChatInputProcessor(Kernel kernel)
        {
            this.chatState = GetChatStateInstance(kernel);   
        }

        public async Task<ModelOutput> Process(ModelInputQA userInput)
        {
            await chatState.AddUserMessageAsync(userInput.Question, () => {});

            var modelOutput = new ModelOutput()
            {
                Input = userInput.Question,
                Output = chatState.Messages.LastOrDefault()?.ToString()!
            };

            return modelOutput;
        }

        private ChatState GetChatStateInstance(Kernel kernel)
        {
            var mockLoggerFactory = Substitute.For<ILoggerFactory>();
            var mockCatalogService = Substitute.For<ICatalogService>();
            var basketState = Substitute.For<IBasketState>();
            ClaimsPrincipal user = default!;
            var productImages = Substitute.For<IProductImageUrlProvider>();

            var brands = new List<CatalogBrand>()
            {
                new(1, "Alpine"),
                new(2, "Raccoon"),
                new(3, "PNW"),
                new(1, "Rocket")
            };

            mockCatalogService.GetBrands().Returns(brands);

            var kidsItems = new List<CatalogItem>()
            {
                new(1, "Kids Mountain Bike", "Mountain Bike 13'", 140, string.Empty, 1, brands.Last(), 1, new CatalogItemType(1, "sports")),
                new(1, "Kids Balaclava", "Youth Balaclava black", 15, string.Empty, 1,  brands.Last(), 1, new CatalogItemType(1, "sports"))
            };
            mockCatalogService.GetCatalogItemsWithSemanticRelevance(Arg.Any<int>(), Arg.Any<int>(), Arg.Is<string>(x => x.Contains("kid"))).Returns(Task.FromResult(new CatalogResult(0, 8, 8, kidsItems)));

            var backpackItems = new List<CatalogItem>()
            {
                new(1, "Alpine Backpack", "Ski Backpack Raccoon Style", 35, string.Empty, 1,  brands.First(), 1, new CatalogItemType(1, "sports"))
            };
            mockCatalogService.GetCatalogItemsWithSemanticRelevance(Arg.Any<int>(), Arg.Any<int>(), "backpack").Returns(Task.FromResult(new CatalogResult(0, 8, 8, backpackItems)));

            

            return new ChatState(
                mockCatalogService,
                basketState,
                user,
                productImages,
                kernel,
                mockLoggerFactory);

           
        }
    }
}
