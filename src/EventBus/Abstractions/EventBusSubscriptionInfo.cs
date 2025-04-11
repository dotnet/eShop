using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Inked.EventBus.Abstractions;

public class EventBusSubscriptionInfo
{
    internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? CreateDefaultTypeResolver()
            : JsonTypeInfoResolver.Combine()
    };

    public Dictionary<string, Type> EventTypes { get; } = [];

    public JsonSerializerOptions JsonSerializerOptions { get; } = new(DefaultSerializerOptions);

#pragma warning disable IL2026
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
    private static IJsonTypeInfoResolver CreateDefaultTypeResolver()
    {
        return new DefaultJsonTypeInfoResolver();
    }
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026
}
