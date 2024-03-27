using System.Diagnostics;

namespace eShop.ClientApp.Animations.Base;

public abstract class AnimationBase : BindableObject
{
    public static readonly BindableProperty TargetProperty = BindableProperty.Create(nameof(Target),
        typeof(VisualElement), typeof(AnimationBase),
        propertyChanged: (bindable, oldValue, newValue) => ((AnimationBase)bindable).Target = (VisualElement)newValue);

    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(string),
        typeof(AnimationBase), "1000",
        propertyChanged: (bindable, oldValue, newValue) => ((AnimationBase)bindable).Duration = (string)newValue);

    public static readonly BindableProperty EasingProperty = BindableProperty.Create(nameof(Easing), typeof(EasingType),
        typeof(AnimationBase), EasingType.Linear,
        propertyChanged: (bindable, oldValue, newValue) => ((AnimationBase)bindable).Easing = (EasingType)newValue);

    public static readonly BindableProperty RepeatForeverProperty = BindableProperty.Create(nameof(RepeatForever),
        typeof(bool), typeof(AnimationBase), false,
        propertyChanged: (bindable, oldValue, newValue) => ((AnimationBase)bindable).RepeatForever = (bool)newValue);

    public static readonly BindableProperty DelayProperty = BindableProperty.Create(nameof(Delay), typeof(int),
        typeof(AnimationBase), 0,
        propertyChanged: (bindable, oldValue, newValue) => ((AnimationBase)bindable).Delay = (int)newValue);

    private bool _isRunning;

    public VisualElement Target
    {
        get => (VisualElement)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public string Duration
    {
        get => (string)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public EasingType Easing
    {
        get => (EasingType)GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    public bool RepeatForever
    {
        get => (bool)GetValue(RepeatForeverProperty);
        set => SetValue(RepeatForeverProperty, value);
    }

    public int Delay
    {
        get => (int)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    protected abstract Task BeginAnimation();

    public async Task Begin()
    {
        try
        {
            if (!_isRunning)
            {
                _isRunning = true;

                await InternalBegin()
                    .ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted)
                    .ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in animation {ex}");
        }
    }

    protected abstract Task ResetAnimation();

    public async Task Reset()
    {
        _isRunning = false;
        await ResetAnimation();
    }

    private async Task InternalBegin()
    {
        if (Delay > 0)
        {
            await Task.Delay(Delay);
        }

        if (!RepeatForever)
        {
            await BeginAnimation();
        }
        else
        {
            do
            {
                await BeginAnimation();
                await ResetAnimation();
            } while (RepeatForever);
        }
    }
}
