using Inked.Submission.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.AI;
using OpenAI;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddAuthenticationServices();
        // Register IHttpContextAccessor
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IAuthorizationHandler, LoggingAuthorizationHandler>();
        // Avoid loading full database config and migrations if startup
        // is being invoked from build-time OpenAPI generation
        if (builder.Environment.IsBuild())
        {
            builder.Services.AddDbContext<SubmissionContext>();
            return;
        }

        builder.AddNpgsqlDbContext<SubmissionContext>("submissiondb",
            configureDbContextOptions: dbContextOptionsBuilder =>
            {
                dbContextOptionsBuilder.UseNpgsql(builder =>
                {
                    builder.UseVector();
                });
            });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IStorageService, LocalStorageService>();
        }
        else
        {
            builder.Services.AddSingleton<IStorageService, AzureBlobStorageService>();
        }

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<SubmissionContext, SubmissionContextSeed>();


        builder.Services.AddTransient<ISubmissionIntegrationEventService, SubmissionIntegrationEventService>();

        // Add the integration services that consume the DbContext
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<SubmissionContext>>();

        builder.AddRabbitMqEventBus("eventbus");

        builder.Services.AddOptions<SubmissionOptions>()
            .BindConfiguration(nameof(SubmissionOptions));

        if (builder.Configuration["OllamaEnabled"] is string ollamaEnabled && bool.Parse(ollamaEnabled))
        {
            builder.AddOllamaApiClient("embedding")
                .AddEmbeddingGenerator();
        }
        else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("openai")))
        {
            builder.AddOpenAIClientFromConfiguration("openai");
            builder.Services.AddEmbeddingGenerator(sp =>
                    sp.GetRequiredService<OpenAIClient>()
                        .AsEmbeddingGenerator(builder.Configuration["AI:OpenAI:EmbeddingModel"]!))
                .UseOpenTelemetry()
                .UseLogging();
        }
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        builder.AddDefaultAuthentication();

        // Blazor auth services
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        services.AddScoped<ISubmissionAI, SubmissionAI>();
    }
}
