namespace Materia.WebUi;

/// <summary>
/// Scoped service that owns the dark-mode flag and notifies subscribers
/// (MudThemeProvider in MudBlazorProviders.razor) when it changes.
/// </summary>
public sealed class ThemeState
{
    public bool IsDarkMode { get; private set; }

    public event Action? OnChanged;

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        OnChanged?.Invoke();
    }

    public void SetDarkMode(bool isDarkMode)
    {
        if (IsDarkMode == isDarkMode) return;
        IsDarkMode = isDarkMode;
        OnChanged?.Invoke();
    }
}
