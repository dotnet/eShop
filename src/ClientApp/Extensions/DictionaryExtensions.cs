namespace eShop.ClientApp;

public static class DictionaryExtensions
{
    public static bool ValueAsBool(this IDictionary<string, object> dictionary, string key, bool defaultValue = false)
    {
        return dictionary.ContainsKey(key) && dictionary[key] is bool dictValue
            ? dictValue
            : defaultValue;
    }

    public static int ValueAsInt(this IDictionary<string, object> dictionary, string key, int defaultValue = 0)
    {
        return dictionary.ContainsKey(key) && dictionary[key] is int intValue
            ? intValue
            : defaultValue;
    }

    public static T ValueAs<T>(this IDictionary<string, object> dictionary, string key, T defaultValue = default)
    {
        return dictionary.ContainsKey(key) && dictionary[key] is T value
            ? value
            : defaultValue;
    }
}
