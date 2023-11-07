using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

internal static class PGExt
{
    public static IResourceBuilder<PgVectorResource> AddPgVector(this IDistributedApplicationBuilder builder, string name)
    {
        var r = new PgVectorResource(name, Guid.NewGuid().ToString());
        return builder.AddResource(r)
                      .WithAnnotation(new ContainerImageAnnotation
                      {
                          Image = "ankane/pgvector",
                          Tag = "latest"
                      })
                      .WithServiceBinding(containerPort: 5432, hostPort: 5432)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(w => WriteContainerInfo(w, r)));
    }

    public static IResourceBuilder<PgVectorDatabase> AddDatabase(this IResourceBuilder<PgVectorResource> builder, string name)
    {
        var db = new PgVectorDatabase(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(db)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(w => WriteDatabaseInfo(w, db)));
    }

    private static void WriteDatabaseInfo(Utf8JsonWriter w, PgVectorDatabase db)
    {
        w.WriteString("type", "pg.database.v0");
        w.WriteString("parent", db.Parent.Name);
        w.WriteString("connectionString", $"{{{db.Parent.Name}.connectionString}};Database={db.Name}");
    }

    private static void WriteContainerInfo(Utf8JsonWriter jsonWriter, PgVectorResource container)
    {
        jsonWriter.WriteString("type", "container.v0");

        if (!container.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        jsonWriter.WriteString("image", image);
        jsonWriter.WriteString("connectionString", "Host={bindings.tcp.host};Port={bindings.tcp.port};Username=postgres;Password={GeneratePassword};");

        WriteEnvironmentVariables(container, jsonWriter);
        WriteBindings(container, jsonWriter);
    }

    private static void WriteEnvironmentVariables(IResource resource, Utf8JsonWriter jsonWriter)
    {
        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("manifest", config);

        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var callbacks))
        {
            jsonWriter.WriteStartObject("env");
            foreach (var callback in callbacks)
            {
                callback.Callback(context);
            }

            foreach (var (key, value) in config)
            {
                jsonWriter.WriteString(key, value);
            }

            jsonWriter.WriteEndObject();
        }
    }

    private static void WriteBindings(IResource resource, Utf8JsonWriter jsonWriter)
    {
        if (resource.TryGetServiceBindings(out var serviceBindings))
        {
            jsonWriter.WriteStartObject("bindings");
            foreach (var serviceBinding in serviceBindings)
            {
                jsonWriter.WriteStartObject(serviceBinding.Name);
                jsonWriter.WriteString("scheme", serviceBinding.UriScheme);
                jsonWriter.WriteString("protocol", serviceBinding.Protocol.ToString().ToLowerInvariant());
                jsonWriter.WriteString("transport", serviceBinding.Transport);

                if (serviceBinding.IsExternal)
                {
                    jsonWriter.WriteBoolean("external", serviceBinding.IsExternal);
                }

                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }
    }
}

class PgVectorResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    private readonly PostgresContainerResource _pg = new(name, password);

    public string? GetConnectionString() => _pg.GetConnectionString();
}


class PgVectorDatabase(string name, PgVectorResource server) : Resource(name), IResourceWithParent<PgVectorResource>, IResourceWithConnectionString
{
    public PgVectorResource Parent { get; } = server;

    public string? GetConnectionString()
    {
        return $"${Parent.GetConnectionString()};Database={Name}"; 
    }
}
