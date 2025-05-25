using Inked.Ordering.API.Application.IntegrationEvents.Events;
using Inked.RefundProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMqEventBus("EventBus")
    .AddSubscription<OrderStatusChangedToReceivedReturnIntegrationEvent,
        OrderStatusChangedToReceivedReturnIntegrationEventHandler>();
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddTransient<IRefundService, RefundService>();

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
