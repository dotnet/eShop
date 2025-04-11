namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string name)
    {
        return configuration.GetValue<T>(name) ?? throw new InvalidOperationException(
            $"Configuration missing value for: {(configuration is IConfigurationSection s ? s.Path + ":" + name : name)}");
    }
}
