namespace eShop.ClientApp.Effects;

public static class ThemeEffects
{
    public static readonly BindableProperty CircleProperty =
        BindableProperty.CreateAttached("Circle", typeof(bool), typeof(ThemeEffects), false,
            propertyChanged: OnChanged<CircleEffect, bool>);

    public static bool GetCircle(BindableObject view)
    {
        return (bool)view.GetValue(CircleProperty);
    }

    public static void SetCircle(BindableObject view, bool circle)
    {
        view.SetValue(CircleProperty, circle);
    }


    private static void OnChanged<TEffect, TProp>(BindableObject bindable, object oldValue, object newValue)
        where TEffect : Effect, new()
    {
        if (bindable is not View view)
        {
            return;
        }

        if (Equals(newValue, default(TProp)))
        {
            var toRemove = view.Effects.FirstOrDefault(e => e is TEffect);
            if (toRemove != null)
            {
                view.Effects.Remove(toRemove);
            }
        }
        else
        {
            view.Effects.Add(new TEffect());
        }
    }

    private class CircleEffect : RoutingEffect
    {
        public CircleEffect()
            : base("ClientApp.CircleEffect")
        {
        }
    }
}
