using System.Windows.Input;

namespace eShop.ClientApp.Controls;

public class ToggleButton : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ToggleButton));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ToggleButton));

    public static readonly BindableProperty CheckedProperty =
        BindableProperty.Create(nameof(Checked), typeof(bool), typeof(ToggleButton), false, BindingMode.TwoWay,
            propertyChanged: OnCheckedChanged);

    public static readonly BindableProperty AnimateProperty =
        BindableProperty.Create(nameof(Animate), typeof(bool), typeof(ToggleButton), false);

    public static readonly BindableProperty CheckedImageProperty =
        BindableProperty.Create(nameof(CheckedImage), typeof(ImageSource), typeof(ToggleButton));

    public static readonly BindableProperty UnCheckedImageProperty =
        BindableProperty.Create(nameof(UnCheckedImage), typeof(ImageSource), typeof(ToggleButton));

    private ICommand _toggleCommand;
    private Image _toggleImage;

    public ToggleButton()
    {
        Initialize();
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool Checked
    {
        get => (bool)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public bool Animate
    {
        get => (bool)GetValue(AnimateProperty);
        set => SetValue(CheckedProperty, value);
    }

    public ImageSource CheckedImage
    {
        get => (ImageSource)GetValue(CheckedImageProperty);
        set => SetValue(CheckedImageProperty, value);
    }

    public ImageSource UnCheckedImage
    {
        get => (ImageSource)GetValue(UnCheckedImageProperty);
        set => SetValue(UnCheckedImageProperty, value);
    }

    public ICommand ToggleCommand =>
        _toggleCommand ??= new Command(() =>
        {
            Checked = !Checked;

            if (Command != null)
            {
                Command.Execute(CommandParameter);
            }
        });

    private void Initialize()
    {
        _toggleImage = new Image();

        Animate = true;

        GestureRecognizers.Add(new TapGestureRecognizer {Command = ToggleCommand});

        _toggleImage.Source = UnCheckedImage;
        Content = _toggleImage;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        _toggleImage.Source = UnCheckedImage;
        Content = _toggleImage;
    }

    private static async void OnCheckedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var toggleButton = (ToggleButton)bindable;

        if (Equals(newValue, null) && !Equals(oldValue, null))
        {
            return;
        }

        toggleButton._toggleImage.Source = toggleButton.Checked
            ? toggleButton.CheckedImage
            : toggleButton.UnCheckedImage;

        toggleButton.Content = toggleButton._toggleImage;

        if (toggleButton.Animate)
        {
            await toggleButton.ScaleTo(0.9, 50, Easing.Linear);
            await Task.Delay(100);
            await toggleButton.ScaleTo(1, 50, Easing.Linear);
        }
    }
}
