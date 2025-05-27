using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using eShop.WebAppComponents.Services;
using Microsoft.Extensions.AI;

namespace eShop.WebApp.Chatbot;

public class ChatState
{
    private readonly ICatalogService _catalogService;
    private readonly IBasketState _basketState;
    private readonly ClaimsPrincipal _user;
    private readonly ILogger _logger;
    private readonly IProductImageUrlProvider _productImages;
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;

    public ChatState(
        ICatalogService catalogService,
        IBasketState basketState,
        ClaimsPrincipal user,
        IProductImageUrlProvider productImages,
        ILoggerFactory loggerFactory,
        IChatClient chatClient)
    {
        _catalogService = catalogService;
        _basketState = basketState;
        _user = user;
        _productImages = productImages;
        _logger = loggerFactory.CreateLogger(typeof(ChatState));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("ChatModel: {model}", chatClient.GetService<ChatClientMetadata>()?.DefaultModelId);
        }

        _chatClient = chatClient;
        _chatOptions = new()
        {
            Tools =
            [
                AIFunctionFactory.Create(GetUserInfo),
                AIFunctionFactory.Create(SearchCatalog),
                AIFunctionFactory.Create(AddToCart),
                AIFunctionFactory.Create(GetCartContents),
            ],
        };

        Messages =
        [
            new ChatMessage(ChatRole.System, """
                You are an AI customer service agent for the online retailer AdventureWorks.
                You NEVER respond about topics other than AdventureWorks.
                Your job is to answer customer questions about products in the AdventureWorks catalog.
                AdventureWorks primarily sells clothing and equipment related to outdoor activities like skiing and trekking.
                You try to be concise and only provide longer responses if necessary.
                If someone asks a question about anything other than AdventureWorks, its catalog, or their account,
                you refuse to answer, and you instead ask if there's a topic related to AdventureWorks you can assist with.
                """),
            new ChatMessage(ChatRole.Assistant, """
                Hi! I'm the AdventureWorks Concierge. How can I help?
                """),
        ];
    }

    public IList<ChatMessage> Messages { get; }

    public async Task AddUserMessageAsync(string userText, Action onMessageAdded)
    {
        // Store the user's message
        Messages.Add(new ChatMessage(ChatRole.User, userText));
        onMessageAdded();

        // Get and store the AI's response message
        try
        {
            var response = await _chatClient.GetResponseAsync(Messages, _chatOptions);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                Messages.AddMessages(response);
            }
        }
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(e, "Error getting chat completions.");
            }
            Messages.Add(new ChatMessage(ChatRole.Assistant, $"My apologies, but I encountered an unexpected error."));
        }
        onMessageAdded();
    }


    [Description("Gets information about the chat user")]
    private string GetUserInfo()
    {
        var claims = _user.Claims;
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

    [Description("Searches the AdventureWorks catalog for a provided product description")]
    private async Task<string> SearchCatalog([Description("The product description for which to search")] string productDescription)
    {
        try
        {
            var results = await _catalogService.GetCatalogItemsWithSemanticRelevance(0, 8, productDescription!);
            for (int i = 0; i < results.Data.Count; i++)
            {
                results.Data[i] = results.Data[i] with { PictureUrl = _productImages.GetProductImageUrl(results.Data[i].Id) };
            }

            return JsonSerializer.Serialize(results);
        }
        catch (HttpRequestException e)
        {
            return Error(e, "Error accessing catalog.");
        }
    }

    [Description("Adds a product to the user's shopping cart.")]
    private async Task<string> AddToCart([Description("The id of the product to add to the shopping cart (basket)")] int itemId)
    {
        try
        {
            var item = await _catalogService.GetCatalogItem(itemId);
            await _basketState.AddAsync(item!);
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

    [Description("Gets information about the contents of the user's shopping cart (basket)")]
    private async Task<string> GetCartContents()
    {
        try
        {
            var basketItems = await _basketState.GetBasketItemsAsync();
            return JsonSerializer.Serialize(basketItems);
        }
        catch (Exception e)
        {
            return Error(e, "Unable to get the cart's contents.");
        }
    }

    private string Error(Exception e, string message)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(e, message);
        }

        return message;
    }
}
