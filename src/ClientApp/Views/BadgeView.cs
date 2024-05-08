using Microsoft.Maui.Controls.Shapes;

namespace eShop.ClientApp.Views;

[ContentProperty(nameof(Content))]
public class BadgeView : Grid
{
    public static BindableProperty ContentProperty =
        BindableProperty.Create(nameof(Content), typeof(View), typeof(BadgeView),
            propertyChanged: OnLayoutPropertyChanged);

    public static BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(BadgeView),
            propertyChanged: OnLayoutPropertyChanged);

    public static BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(BadgeView),
            propertyChanged: OnLayoutPropertyChanged);

    public static BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(BadgeView), 10.0d,
            propertyChanged: OnLayoutPropertyChanged);

    public static BindableProperty BadgeColorProperty =
        BindableProperty.Create(nameof(BadgeColor), typeof(Color), typeof(BadgeView),
            propertyChanged: OnLayoutPropertyChanged);

    private readonly Label _badgeIndicator;
    private readonly Border _border;
    private readonly RoundRectangle _borderShape;

    public BadgeView()
    {
        _badgeIndicator =
            new Label
            {
                Padding = 4,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

        _borderShape = new RoundRectangle();

        _border =
            new Border
            {
                StrokeShape = _borderShape,
                Content = _badgeIndicator,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                ZIndex = 10
            };

        Children.Add(_border);

        UpdateLayout();
    }

    public View Content
    {
        get => (View)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public Color BadgeColor
    {
        get => (Color)GetValue(BadgeColorProperty);
        set => SetValue(BadgeColorProperty, value);
    }

    private static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        (bindable as BadgeView)?.UpdateLayout();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);

        _border.SizeChanged -= BadgeIndicatorSizeChanged;

        if (args.NewHandler is not null)
        {
            _border.SizeChanged += BadgeIndicatorSizeChanged;
        }
    }

    private void BadgeIndicatorSizeChanged(object sender, EventArgs e)
    {
        var halfHeight = _border.Height * .5f;
        _border.MinimumWidthRequest = _border.Height;
        _borderShape.CornerRadius = halfHeight;

        if (Content is not null)
        {
            Content.Margin = halfHeight;
        }
    }

    private void UpdateLayout()
    {
        BatchBegin();
        _border.BatchBegin();
        _badgeIndicator.BatchBegin();

        if (Content is not null && Content.Parent != this)
        {
            Content.ZIndex = 1;
            Children.Add(Content);
        }

        _border.BackgroundColor = BadgeColor;

        _badgeIndicator.Text = Text;
        _badgeIndicator.TextColor = TextColor;
        _badgeIndicator.FontSize = FontSize;

        _border.BatchCommit();
        _badgeIndicator.BatchCommit();
        BatchCommit();
    }
}
