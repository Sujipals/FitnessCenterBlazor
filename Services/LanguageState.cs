namespace FitnessCenterBlazor.Services
{
    public class LanguageState
    {
        public string CurrentLanguage { get; private set; } = "English";

        public event Action? LanguageChanged;

        public void SetLanguage(string language)
        {
            CurrentLanguage = language;

            LanguageChanged?.Invoke();
        }
    }
}