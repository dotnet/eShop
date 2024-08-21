using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;
using eShop.WebAppComponents.Services;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace eShop.WebApp.Chatbot;

public class ChatState
{
    private readonly ICatalogService _catalogService;
    private readonly IBasketState _basketState;
    private readonly ClaimsPrincipal _user;
    private readonly ILogger _logger;
    private readonly Kernel _kernel;
    private readonly IProductImageUrlProvider _productImages;
    private readonly OpenAIPromptExecutionSettings _aiSettings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

    public ChatState(ICatalogService catalogService, IBasketState basketState, ClaimsPrincipal user, IProductImageUrlProvider productImages, Kernel kernel, ILoggerFactory loggerFactory)
    {
        _catalogService = catalogService;
        _basketState = basketState;
        _user = user;
        _productImages = productImages;
        _logger = loggerFactory.CreateLogger(typeof(ChatState));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var completionService = kernel.GetRequiredService<IChatCompletionService>();
            _logger.LogDebug("ChatName: {model}", completionService.Attributes["DeploymentName"]);
        }

        _kernel = kernel;
        _kernel.Plugins.AddFromObject(new CatalogInteractions(this));

        Messages = new ChatHistory("""
            You are an AI customer service agent for the online retailer AdventureWorks.
            You NEVER respond about topics other than AdventureWorks.
            Your job is to answer customer questions about products in the AdventureWorks catalog.
            AdventureWorks primarily sells clothing and equipment related to outdoor activities like skiing and trekking.
            You try to be concise and only provide longer responses if necessary.
            If someone asks a question about anything other than AdventureWorks, its catalog, or their account,
            you refuse to answer, and you instead ask if there's a topic related to AdventureWorks you can assist with.
            """);
        Messages.AddAssistantMessage("Hi! I'm the AdventureWorks Concierge. How can I help?");
    }

    public ChatHistory Messages { get; }

    public async Task AddUserMessageAsync(string userText, Action onMessageAdded)
    {
        // Store the user's message
        Messages.AddUserMessage(userText);
        onMessageAdded();

        // Get and store the AI's response message
        try
        {
            ChatMessageContent response = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(Messages, _aiSettings, _kernel);
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                Messages.Add(response);
            }
        }
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(e, "Error getting chat completions.");
            }
            Messages.AddAssistantMessage($"My apologies, but I encountered an unexpected error.");
        }
        onMessageAdded();
    }

    private sealed class CatalogInteractions(ChatState chatState)
    {
        [KernelFunction, Description("Gets information about the chat user")]
        public string GetUserInfo()
        {
            var claims = chatState._user.Claims;
            return JsonSerializer.Serialize(new
            {
                Name = GetValue(claims, "name"),
                LastName = GetValue(claims, "last_name"),
                Street = GetValue(claims, "address_street"),
                City = GetValue(claims, "address_city"),
                State = GetValue(claims, "address_state"),
                ZipCode = GetValue(claims, "address_zip_code"),
                Country = GetValue(claims, "address_country"),
                Email = GetValue(claims, "email"),
                PhoneNumber = GetValue(claims, "phone_number"),
            });

            static string GetValue(IEnumerable<Claim> claims, string claimType) =>
                claims.FirstOrDefault(x => x.Type == claimType)?.Value ?? "";
        }

        [KernelFunction, Description("Searches the AdventureWorks catalog for a provided product description")]
        public async Task<string> SearchCatalog([Description("The product description for which to search")] string productDescription)
        {
            try
            {
                var results = await chatState._catalogService.GetCatalogItemsWithSemanticRelevance(0, 8, productDescription!);
                for (int i = 0; i < results.Data.Count; i++)
                {
                    results.Data[i] = results.Data[i] with { PictureUrl = chatState._productImages.GetProductImageUrl(results.Data[i].Id) };
                }

                return JsonSerializer.Serialize(results);
            }
            catch (HttpRequestException e)
            {
                return Error(e, "Error accessing catalog.");
            }
        }

        [KernelFunction, Description("Adds a product to the user's shopping cart.")]
        public async Task<string> AddToCart([Description("The id of the product to add to the shopping cart (basket)")] int itemId)
        {
            try
            {
                var item = await chatState._catalogService.GetCatalogItem(itemId);
                await chatState._basketState.AddAsync(item!);
                return "Item added to shopping cart.";
            }
            catch (Grpc.Core.RpcException e) when (e.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
            {
                return "Unable to add an item to the cart. You must be logged in.";
            }
            catch (Exception e)
            {
                return Error(e, "Unable to add the item to the cart.");
            }
        }

        [KernelFunction, Description("Gets information about the contents of the user's shopping cart (basket)")]
        public async Task<string> GetCartContents()
        {
            try
            {
                var basketItems = await chatState._basketState.GetBasketItemsAsync();
                return JsonSerializer.Serialize(basketItems);
            }
            catch (Exception e)
            {
                return Error(e, "Unable to get the cart's contents.");
            }
        }

        private string Error(Exception e, string message)
        {
            if (chatState._logger.IsEnabled(LogLevel.Error))
            {
                chatState._logger.LogError(e, message);
            }

            return message;
        }
    }
}
