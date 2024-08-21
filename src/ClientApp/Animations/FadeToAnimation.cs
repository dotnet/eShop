using eShop.ClientApp.Animations.Base;
using eShop.ClientApp.Helpers;

namespace eShop.ClientApp.Animations;

public class FadeToAnimation : AnimationBase
{
    public static readonly BindableProperty OpacityProperty =
        BindableProperty.Create(nameof(Opacity), typeof(double), typeof(FadeToAnimation), 0.0d,
            propertyChanged: (bindable, oldValue, newValue) =>
                ((FadeToAnimation)bindable).Opacity = (double)newValue);

    public double Opacity
    {
        get => (double)GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }

    protected override Task BeginAnimation()
    {
        if (Target == null)
        {
            throw new NullReferenceException("Null Target property.");
        }

        return Target.FadeTo(Opacity, Convert.ToUInt32(Duration), EasingHelper.GetEasing(Easing));
    }

    protected override Task ResetAnimation()
    {
        if (Target == null)
        {
            throw new NullReferenceException("Null Target property.");
        }

        return Target.FadeTo(0, 0);
    }
}

public class FadeInAnimation : AnimationBase
{
    public enum FadeDirection
    {
        Up,
        Down
    }

    public static readonly BindableProperty DirectionProperty =
        BindableProperty.Create(nameof(Direction), typeof(FadeDirection), typeof(FadeInAnimation), FadeDirection.Up,
            propertyChanged: (bindable, oldValue, newValue) =>
                ((FadeInAnimation)bindable).Direction = (FadeDirection)newValue);

    public FadeDirection Direction
    {
        get => (FadeDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    protected override Task BeginAnimation()
    {
        if (Target == null)
        {
            throw new NullReferenceException("Null Target property.");
        }

        Target.Dispatcher.Dispatch(() => Target.Animate("FadeIn", FadeIn(), 16, Convert.ToUInt32(Duration)));

        return Task.CompletedTask;
    }

    protected override Task ResetAnimation()
    {
        if (Target == null)
        {
            throw new NullReferenceException("Null Target property.");
        }

        Target.Dispatcher.Dispatch(() => Target.FadeTo(0, 0));

        return Task.CompletedTask;
    }

    internal Animation FadeIn()
    {
        var animation = new Animation();

        animation.WithConcurrent(f => Target.Opacity = f, 0, 1, Microsoft.Maui.Easing.CubicOut);

        animation.WithConcurrent(
            f => Target.TranslationY = f,
            Target.TranslationY + (Direction == FadeDirection.Up ? 50 : -50), Target.TranslationY,
            Microsoft.Maui.Easing.CubicOut);

        return animation;
    }
}

public class FadeOutAnimation : AnimationBase
{
    public enum FadeDirection
    {
        Up,
        Down
    }

    public static readonly BindableProperty DirectionProperty =
        BindableProperty.Create(nameof(Direction), typeof(FadeDirection), typeof(FadeOutAnimation), FadeDirection.Up,
            propertyChanged: (bindable, oldValue, newValue) =>
                ((FadeOutAnimation)bindable).Direction = (FadeDirection)newValue);

    public FadeDirection Direction
    {
        get => (FadeDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    protected override Task BeginAnimation()
    {
        if (Target == null)
        {
            throw new NullReferenceException("Null Target property.");
        }

        Target.Dispatcher.Dispatch(() => Target.Animate("FadeOut", FadeOut(), 16, Convert.ToUInt32(Duration)));

        return Task.CompletedTask;
    }

    protected override Task ResetAnimation()
    {
        if (Target == null)
        {
            throw new NullReferenceException("Null Target property.");
        }

        Target.Dispatcher.Dispatch(() => Target.FadeTo(0, 0));

        return Task.CompletedTask;
    }

    internal Animation FadeOut()
    {
        Animation animation = new();

        animation.WithConcurrent(
            f => Target.Opacity = f,
            1, 0);

        animation.WithConcurrent(
            f => Target.TranslationY = f,
            Target.TranslationY, Target.TranslationY + (Direction == FadeDirection.Up ? 50 : -50));

        return animation;
    }
}
