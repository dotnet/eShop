namespace eShop.WebApp.Services
{
    public class ThemeState()
    {
        public bool IsDark { get; private set; }

        public event Action? OnChange;

        public void SetIsDark(bool data)
        {
            IsDark = data;
            NotifyStateChanged();
        }



        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
