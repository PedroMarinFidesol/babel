using Babel.WebUI.Models;
using Microsoft.JSInterop;
using MudBlazor;

namespace Babel.WebUI.Services;

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private AppThemeType _currentTheme = AppThemeType.Light;
    private bool _isDarkMode = false;
    private const string LocalStorageKey = "babel-theme-preference";

    public AppThemeType CurrentTheme => _currentTheme;
    public bool IsDarkMode => _isDarkMode;
    public MudTheme CurrentMudTheme => AppTheme.GetMudTheme(_currentTheme);
    public string CurrentThemeName => AppTheme.GetThemeName(_currentTheme);
    public string CurrentThemeColor => AppTheme.GetThemeColor(_currentTheme);
    public string CurrentThemeIcon => AppTheme.GetThemeIcon(_currentTheme);
    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task LoadThemePreferenceAsync()
    {
        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
            
            if (string.IsNullOrEmpty(savedTheme) || !Enum.TryParse<AppThemeType>(savedTheme, out var theme))
            {
                theme = AppThemeType.Light;
            }
            
            await SetThemeAsync(theme);
        }
        catch
        {
            _currentTheme = AppThemeType.Light;
            _isDarkMode = false;
        }
    }

    public async Task SetThemeAsync(AppThemeType theme)
    {
        _currentTheme = theme;
        _isDarkMode = AppTheme.IsDarkTheme(theme);
        
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, theme.ToString());
            
            var themeClass = $"theme-{theme.ToString().ToLower()}";
            await _jsRuntime.InvokeVoidAsync("babel.applyTheme", themeClass, _isDarkMode);
        }
        catch
        {
        }
        
        OnThemeChanged?.Invoke();
    }
}
