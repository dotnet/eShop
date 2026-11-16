using eShop.Ordering.API.Application.DomainEventHandlers;
using FluentValidation;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        
        // Add the authentication services to DI
        builder.AddDefaultAuthentication();

        // Pooling is disabled because of the following error:
        // Unhandled exception. System.InvalidOperationException:
        // The DbContext of type 'OrderingContext' cannot be pooled because it does not have a public constructor accepting a single parameter of type DbContextOptions or has more than one constructor.
        services.AddDbContext<OrderingContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("orderingdb"));
        });
        builder.EnrichNpgsqlDbContext<OrderingContext>();

        services.AddMigration<OrderingContext, OrderingContextSeed>();

        // Add the integration services that consume the DbContext
        services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<OrderingContext>>();

        services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddEventBusSubscriptions();

        services.AddHttpContextAccessor();
        services.AddTransient<IIdentityService, IdentityService>();

        // Register the command handlers. Each handler is wrapped in the logging, validation and transaction pipeline.
        services.AddCommandHandler<CreateOrderCommand, bool, CreateOrderCommandHandler>();
        services.AddCommandHandler<IdentifiedCommand<CreateOrderCommand, bool>, bool, CreateOrderIdentifiedCommandHandler>();
        services.AddCommandHandler<CancelOrderCommand, bool, CancelOrderCommandHandler>();
        services.AddCommandHandler<IdentifiedCommand<CancelOrderCommand, bool>, bool, CancelOrderIdentifiedCommandHandler>();
        services.AddCommandHandler<ShipOrderCommand, bool, ShipOrderCommandHandler>();
        services.AddCommandHandler<IdentifiedCommand<ShipOrderCommand, bool>, bool, ShipOrderIdentifiedCommandHandler>();
        services.AddCommandHandler<CreateOrderDraftCommand, OrderDraftDTO, CreateOrderDraftCommandHandler>();
        services.AddCommandHandler<SetAwaitingValidationOrderStatusCommand, bool, SetAwaitingValidationOrderStatusCommandHandler>();
        services.AddCommandHandler<SetPaidOrderStatusCommand, bool, SetPaidOrderStatusCommandHandler>();
        services.AddCommandHandler<SetStockConfirmedOrderStatusCommand, bool, SetStockConfirmedOrderStatusCommandHandler>();
        services.AddCommandHandler<SetStockRejectedOrderStatusCommand, bool, SetStockRejectedOrderStatusCommandHandler>();

        // Register the domain event handlers, which are dispatched by the OrderingContext when entities are saved
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddDomainEventHandler<OrderStartedDomainEvent, ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler>();
        services.AddDomainEventHandler<BuyerAndPaymentMethodVerifiedDomainEvent, UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler>();
        services.AddDomainEventHandler<OrderStatusChangedToAwaitingValidationDomainEvent, OrderStatusChangedToAwaitingValidationDomainEventHandler>();
        services.AddDomainEventHandler<OrderStatusChangedToStockConfirmedDomainEvent, OrderStatusChangedToStockConfirmedDomainEventHandler>();
        services.AddDomainEventHandler<OrderStatusChangedToPaidDomainEvent, OrderStatusChangedToPaidDomainEventHandler>();
        services.AddDomainEventHandler<OrderShippedDomainEvent, OrderShippedDomainEventHandler>();
        services.AddDomainEventHandler<OrderCancelledDomainEvent, OrderCancelledDomainEventHandler>();

        // Register the command validators for the validator behavior (validators based on FluentValidation library)
        services.AddValidatorsFromAssemblyContaining<CancelOrderCommandValidator>();

        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IRequestManager, RequestManager>();
    }

    private static IServiceCollection AddCommandHandler<TCommand, TResult, THandler>(this IServiceCollection services)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        services.AddTransient<THandler>();

        // Compose the pipeline, from outermost to innermost: logging -> validation -> transaction -> handler
        services.AddTransient<ICommandHandler<TCommand, TResult>>(sp =>
            new LoggingBehavior<TCommand, TResult>(
                new ValidatorBehavior<TCommand, TResult>(
                    new TransactionBehavior<TCommand, TResult>(
                        sp.GetRequiredService<THandler>(),
                        sp.GetRequiredService<OrderingContext>(),
                        sp.GetRequiredService<IOrderingIntegrationEventService>(),
                        sp.GetRequiredService<ILogger<TransactionBehavior<TCommand, TResult>>>()),
                    sp.GetServices<IValidator<TCommand>>(),
                    sp.GetRequiredService<ILogger<ValidatorBehavior<TCommand, TResult>>>()),
                sp.GetRequiredService<ILogger<LoggingBehavior<TCommand, TResult>>>()));

        return services;
    }

    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<GracePeriodConfirmedIntegrationEvent, GracePeriodConfirmedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStockConfirmedIntegrationEvent, OrderStockConfirmedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStockRejectedIntegrationEvent, OrderStockRejectedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderPaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>();
    }
}
