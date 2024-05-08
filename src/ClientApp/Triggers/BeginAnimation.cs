using eShop.ClientApp.Animations.Base;

namespace eShop.ClientApp.Triggers;

public class BeginAnimation : TriggerAction<VisualElement>
{
    public AnimationBase Animation { get; set; }

    protected override async void Invoke(VisualElement sender)
    {
        if (Animation != null)
        {
            await Animation.Begin();
        }
    }
}
