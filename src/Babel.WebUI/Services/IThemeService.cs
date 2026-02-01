using Babel.WebUI.Models;
using MudBlazor;

namespace Babel.WebUI.Services;

public interface IThemeService
{
    AppThemeType CurrentTheme { get; }
    bool IsDarkMode { get; }
    MudTheme CurrentMudTheme { get; }
    string CurrentThemeName { get; }
    string CurrentThemeColor { get; }
    string CurrentThemeIcon { get; }
    event Action? OnThemeChanged;

    Task LoadThemePreferenceAsync();
    Task SetThemeAsync(AppThemeType theme);
}
