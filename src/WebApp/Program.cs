using eShop.WebAppComponents.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.AddAuthenticationServices();
builder.AddRabbitMqEventBus("EventBus")
       .AddEventBusSubscriptions();

// Application services
builder.Services.AddScoped<BasketState>();
builder.Services.AddScoped<LogOutService>();
builder.Services.AddSingleton<BasketService>();
builder.Services.AddSingleton<OrderStatusNotificationService>();

// Backend services
builder.Services.AddGrpcClient<Basket.BasketClient>(o => o.Address = new("http://basket-api")).AddAuthToken();
builder.Services.AddHttpClient<CatalogService>(o => o.BaseAddress = new("http://catalog-api")).AddAuthToken();
builder.Services.AddHttpClient<OrderingService>(o => o.BaseAddress = new("http://ordering-api")).AddAuthToken();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAntiforgery();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
