using MudBlazor;

namespace Babel.WebUI.Models;

public enum AppThemeType
{
    Light,
    Dark,
    Midnight,
    Forest,
    Ocean,
    Sunset,
    Aurora,
    Hacker
}

public static class AppTheme
{
    private static readonly MudTheme LightTheme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#3B82F6",
            Secondary = "#6B7280",
            Tertiary = "#8B5CF6",
            Error = "#DC2626",
            Info = "#0284C7",
            Success = "#059669",
            Warning = "#D97706",
            Dark = "#1F2937",
            Background = "#F9FAFB",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1F2937",
            AppbarBackground = "#3B82F6",
            AppbarText = "#FFFFFF",
            TextPrimary = "#111827",
            TextSecondary = "#6B7280",
            ActionDefault = "#3B82F6",
            ActionDisabled = "#9CA3AF",
            ActionDisabledBackground = "#E5E7EB",
            Divider = "#E5E7EB",
            TableHover = "rgba(59, 130, 246, 0.08)",
            TableStriped = "rgba(59, 130, 246, 0.04)",
            LinesDefault = "#E5E7EB"
        },
        Typography = new Typography(),
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "4px"
        }
    };

    private static readonly MudTheme DarkTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#3B82F6",
            Secondary = "#6B7280",
            Tertiary = "#8B5CF6",
            Error = "#DC2626",
            Info = "#0284C7",
            Success = "#059669",
            Warning = "#D97706",
            Dark = "#111827",
            Background = "#121212",
            Surface = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
            DrawerText = "#F9FAFB",
            AppbarBackground = "#1E3A8A",
            AppbarText = "#FFFFFF",
            TextPrimary = "#F9FAFB",
            TextSecondary = "#9CA3AF",
            ActionDefault = "#3B82F6",
            ActionDisabled = "#6B7280",
            ActionDisabledBackground = "rgba(255, 255, 255, 0.12)",
            Divider = "rgba(255, 255, 255, 0.12)",
            TableHover = "rgba(59, 130, 246, 0.08)",
            TableStriped = "rgba(255, 255, 255, 0.03)",
            LinesDefault = "rgba(255, 255, 255, 0.12)"
        },
        Typography = LightTheme.Typography,
        LayoutProperties = LightTheme.LayoutProperties
    };

    private static readonly MudTheme MidnightTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#3B82F6",
            Secondary = "#8B5CF6",
            Tertiary = "#6366F1",
            Error = "#EF4444",
            Info = "#0EA5E9",
            Success = "#10B981",
            Warning = "#F59E0B",
            Dark = "#0F172A",
            Background = "#0F172A",
            Surface = "#1E293B",
            DrawerBackground = "#1E293B",
            DrawerText = "#F1F5F9",
            AppbarBackground = "#1E293B",
            AppbarText = "#F1F5F9",
            TextPrimary = "#F1F5F9",
            TextSecondary = "#94A3B8",
            ActionDefault = "#3B82F6",
            ActionDisabled = "#64748B",
            ActionDisabledBackground = "rgba(255, 255, 255, 0.08)",
            Divider = "rgba(148, 163, 184, 0.12)",
            TableHover = "rgba(59, 130, 246, 0.1)",
            TableStriped = "rgba(59, 130, 246, 0.05)",
            LinesDefault = "rgba(148, 163, 184, 0.12)"
        },
        Typography = LightTheme.Typography,
        LayoutProperties = LightTheme.LayoutProperties
    };

    private static readonly MudTheme ForestTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#059669",
            Secondary = "#92400E",
            Tertiary = "#15803D",
            Error = "#DC2626",
            Info = "#0891B2",
            Success = "#10B981",
            Warning = "#EA580C",
            Dark = "#064E3B",
            Background = "#064E3B",
            Surface = "#065F46",
            DrawerBackground = "#065F46",
            DrawerText = "#ECFDF5",
            AppbarBackground = "#064E3B",
            AppbarText = "#ECFDF5",
            TextPrimary = "#D1FAE5",
            TextSecondary = "#6EE7B7",
            ActionDefault = "#059669",
            ActionDisabled = "#34D399",
            ActionDisabledBackground = "rgba(0, 0, 0, 0.2)",
            Divider = "rgba(110, 231, 183, 0.15)",
            TableHover = "rgba(5, 150, 105, 0.1)",
            TableStriped = "rgba(5, 150, 105, 0.05)",
            LinesDefault = "rgba(110, 231, 183, 0.15)"
        },
        Typography = LightTheme.Typography,
        LayoutProperties = LightTheme.LayoutProperties
    };

    private static readonly MudTheme OceanTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#0891B2",
            Secondary = "#155E75",
            Tertiary = "#0E7490",
            Error = "#DC2626",
            Info = "#06B6D4",
            Success = "#059669",
            Warning = "#EA580C",
            Dark = "#164E63",
            Background = "#164E63",
            Surface = "#0E7490",
            DrawerBackground = "#0E7490",
            DrawerText = "#CFFAFE",
            AppbarBackground = "#164E63",
            AppbarText = "#CFFAFE",
            TextPrimary = "#CFFAFE",
            TextSecondary = "#67E8F9",
            ActionDefault = "#0891B2",
            ActionDisabled = "#22D3EE",
            ActionDisabledBackground = "rgba(0, 0, 0, 0.2)",
            Divider = "rgba(103, 232, 249, 0.15)",
            TableHover = "rgba(8, 145, 178, 0.1)",
            TableStriped = "rgba(8, 145, 178, 0.05)",
            LinesDefault = "rgba(103, 232, 249, 0.15)"
        },
        Typography = LightTheme.Typography,
        LayoutProperties = LightTheme.LayoutProperties
    };

    private static readonly MudTheme SunsetTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#EA580C",
            Secondary = "#A21CAF",
            Tertiary = "#7C2D12",
            Error = "#DC2626",
            Info = "#0891B2",
            Success = "#059669",
            Warning = "#EA580C",
            Dark = "#7C2D12",
            Background = "#7C2D12",
            Surface = "#9A3412",
            DrawerBackground = "#9A3412",
            DrawerText = "#FFEDD5",
            AppbarBackground = "#7C2D12",
            AppbarText = "#FFEDD5",
            TextPrimary = "#FFEDD5",
            TextSecondary = "#FDBA74",
            ActionDefault = "#EA580C",
            ActionDisabled = "#FB923C",
            ActionDisabledBackground = "rgba(0, 0, 0, 0.2)",
            Divider = "rgba(253, 186, 116, 0.15)",
            TableHover = "rgba(234, 88, 12, 0.1)",
            TableStriped = "rgba(234, 88, 12, 0.05)",
            LinesDefault = "rgba(253, 186, 116, 0.15)"
        },
        Typography = LightTheme.Typography,
        LayoutProperties = LightTheme.LayoutProperties
    };

    private static readonly MudTheme AuroraTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#00FF88",
            Secondary = "#00CCFF",
            Tertiary = "#FF00FF",
            Error = "#FF4444",
            Info = "#00CCFF",
            Success = "#00FF88",
            Warning = "#FFAA00",
            Dark = "#000D0D",
            Background = "#000D0D",
            Surface = "#001A1A",
            DrawerBackground = "#001A1A",
            DrawerText = "#00FF88",
            AppbarBackground = "#001A1A",
            AppbarText = "#00FF88",
            TextPrimary = "#00FF88",
            TextSecondary = "#00CCFF",
            ActionDefault = "#00FF88",
            ActionDisabled = "#006644",
            ActionDisabledBackground = "rgba(0, 255, 136, 0.1)",
            Divider = "rgba(0, 255, 136, 0.2)",
            TableHover = "rgba(0, 255, 136, 0.08)",
            TableStriped = "rgba(0, 255, 136, 0.04)",
            LinesDefault = "rgba(0, 255, 136, 0.2)"
        },
        Typography = LightTheme.Typography,
        LayoutProperties = LightTheme.LayoutProperties
    };

    private static readonly MudTheme HackerTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#00FF00",
            Secondary = "#008000",
            Tertiary = "#00AA00",
            Error = "#FF0000",
            Info = "#00FFFF",
            Success = "#00FF00",
            Warning = "#FFFF00",
            Dark = "#000000",
            Background = "#000000",
            Surface = "#0A0A0A",
            DrawerBackground = "#0A0A0A",
            DrawerText = "#00FF00",
            AppbarBackground = "#000000",
            AppbarText = "#00FF00",
            TextPrimary = "#00FF00",
            TextSecondary = "#00AA00",
            ActionDefault = "#00FF00",
            ActionDisabled = "#005500",
            ActionDisabledBackground = "rgba(0, 255, 0, 0.1)",
            Divider = "#00FF00",
            TableHover = "rgba(0, 255, 0, 0.08)",
            TableStriped = "rgba(0, 255, 0, 0.04)",
            LinesDefault = "#00FF00"
        },
        Typography = new Typography(),
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "0px"
        }
    };

    public static MudTheme GetMudTheme(AppThemeType themeType)
    {
        return themeType switch
        {
            AppThemeType.Light => LightTheme,
            AppThemeType.Dark => DarkTheme,
            AppThemeType.Midnight => MidnightTheme,
            AppThemeType.Forest => ForestTheme,
            AppThemeType.Ocean => OceanTheme,
            AppThemeType.Sunset => SunsetTheme,
            AppThemeType.Aurora => AuroraTheme,
            AppThemeType.Hacker => HackerTheme,
            _ => LightTheme
        };
    }

    public static string GetThemeName(AppThemeType themeType)
    {
        return themeType switch
        {
            AppThemeType.Light => "Claro",
            AppThemeType.Dark => "Oscuro",
            AppThemeType.Midnight => "Medianoche",
            AppThemeType.Forest => "Bosque",
            AppThemeType.Ocean => "Océano",
            AppThemeType.Sunset => "Atardecer",
            AppThemeType.Aurora => "Aurora",
            AppThemeType.Hacker => "Hacker",
            _ => "Claro"
        };
    }

    public static string GetThemeColor(AppThemeType themeType)
    {
        return themeType switch
        {
            AppThemeType.Light => "#3B82F6",
            AppThemeType.Dark => "#3B82F6",
            AppThemeType.Midnight => "#3B82F6",
            AppThemeType.Forest => "#059669",
            AppThemeType.Ocean => "#0891B2",
            AppThemeType.Sunset => "#EA580C",
            AppThemeType.Aurora => "#00FF88",
            AppThemeType.Hacker => "#00FF00",
            _ => "#3B82F6"
        };
    }

    public static string GetThemeIcon(AppThemeType themeType)
    {
        return themeType switch
        {
            AppThemeType.Light => Icons.Material.Filled.LightMode,
            AppThemeType.Dark => Icons.Material.Filled.DarkMode,
            AppThemeType.Midnight => Icons.Material.Filled.NightShelter,
            AppThemeType.Forest => Icons.Material.Filled.Forest,
            AppThemeType.Ocean => Icons.Material.Filled.Water,
            AppThemeType.Sunset => Icons.Material.Filled.WbSunny,
            AppThemeType.Aurora => Icons.Material.Filled.AutoAwesome,
            AppThemeType.Hacker => Icons.Material.Filled.Terminal,
            _ => Icons.Material.Filled.LightMode
        };
    }

    public static bool IsDarkTheme(AppThemeType themeType)
    {
        return themeType switch
        {
            AppThemeType.Light => false,
            AppThemeType.Dark => true,
            AppThemeType.Midnight => true,
            AppThemeType.Forest => true,
            AppThemeType.Ocean => true,
            AppThemeType.Sunset => true,
            AppThemeType.Aurora => true,
            AppThemeType.Hacker => true,
            _ => false
        };
    }

    public static List<AppThemeType> GetAllThemes()
    {
        return Enum.GetValues<AppThemeType>().ToList();
    }
}
