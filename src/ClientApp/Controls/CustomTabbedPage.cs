namespace eShop.ClientApp.Controls;

public class CustomTabbedPage : TabbedPage
{
    public static BindableProperty BadgeTextProperty =
        BindableProperty.CreateAttached("BadgeText", typeof(string), typeof(CustomTabbedPage), default(string));

    public static BindableProperty BadgeColorProperty =
        BindableProperty.CreateAttached("BadgeColor", typeof(Color), typeof(CustomTabbedPage), Colors.Transparent);

    public static string GetBadgeText(BindableObject view)
    {
        return (string)view.GetValue(BadgeTextProperty);
    }

    public static void SetBadgeText(BindableObject view, string value)
    {
        view.SetValue(BadgeTextProperty, value);
    }

    public static Color GetBadgeColor(BindableObject view)
    {
        return (Color)view.GetValue(BadgeColorProperty);
    }

    public static void SetBadgeColor(BindableObject view, Color value)
    {
        view.SetValue(BadgeColorProperty, value);
    }
}
