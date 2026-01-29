using MudBlazor;

namespace GolfTrackerApp.Web.Theme;

/// <summary>
/// Premium MudBlazor theme for the Golf Tracker App.
/// Designed for a professional, modern appearance with refined colors and typography.
/// </summary>
public static class GolfTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            // Primary - Deep Forest Green (Premium Golf)
            Primary = "#2e7d32",
            PrimaryContrastText = "#ffffff",
            PrimaryDarken = "#1b5e20",
            PrimaryLighten = "#4caf50",
            
            // Secondary - Gold/Amber (Tournament Feel)
            Secondary = "#ffc107",
            SecondaryContrastText = "#212121",
            SecondaryDarken = "#ff8f00",
            SecondaryLighten = "#ffd54f",
            
            // Tertiary
            Tertiary = "#1976d2",
            TertiaryContrastText = "#ffffff",
            
            // Status Colors
            Success = "#43a047",
            SuccessContrastText = "#ffffff",
            Warning = "#fb8c00",
            WarningContrastText = "#ffffff",
            Error = "#e53935",
            ErrorContrastText = "#ffffff",
            Info = "#1e88e5",
            InfoContrastText = "#ffffff",
            
            // Backgrounds - Subtle and clean
            Background = "#fafafa",
            BackgroundGray = "#f5f5f5",
            Surface = "#ffffff",
            
            // Text - High contrast for readability
            TextPrimary = "#212121",
            TextSecondary = "#757575",
            TextDisabled = "#9e9e9e",
            
            // App Bar - Premium green
            AppbarBackground = "#2e7d32",
            AppbarText = "#ffffff",
            
            // Drawer
            DrawerBackground = "#ffffff",
            DrawerText = "#212121",
            DrawerIcon = "#2e7d32",
            
            // Dividers
            Divider = "#e0e0e0",
            DividerLight = "#f5f5f5",
            
            // Actions
            ActionDefault = "#616161",
            ActionDisabled = "#bdbdbd",
            ActionDisabledBackground = "#eeeeee",
            
            // Table
            TableHover = "#f5f5f5",
            TableStriped = "#fafafa",
            
            // Overlay
            OverlayDark = "rgba(0,0,0,0.6)",
            OverlayLight = "rgba(255,255,255,0.8)"
        },
        
        PaletteDark = new PaletteDark
        {
            // Primary Golf Green (lighter for dark mode)
            Primary = "#66bb6a",
            PrimaryContrastText = "#000000",
            PrimaryDarken = "#43a047",
            PrimaryLighten = "#81c784",
            
            // Secondary
            Secondary = "#ffd54f",
            SecondaryContrastText = "#212121",
            
            // Tertiary
            Tertiary = "#64b5f6",
            TertiaryContrastText = "#000000",
            
            // Status Colors
            Success = "#81c784",
            Warning = "#ffb74d",
            Error = "#ef5350",
            Info = "#64b5f6",
            
            // Backgrounds
            Background = "#121212",
            BackgroundGray = "#1e1e1e",
            Surface = "#1e1e1e",
            
            // Text
            TextPrimary = "#ffffff",
            TextSecondary = "#b3b3b3",
            TextDisabled = "#757575",
            
            // App Bar
            AppbarBackground = "#1e1e1e",
            AppbarText = "#ffffff",
            
            // Drawer
            DrawerBackground = "#1e1e1e",
            DrawerText = "#ffffff",
            DrawerIcon = "#81c784",
            
            // Dividers
            Divider = "#424242",
            DividerLight = "#303030"
        },
        
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Roboto", "Helvetica Neue", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "normal"
            },
            H1 = new H1Typography
            {
                FontSize = "2.5rem",
                FontWeight = "700",
                LineHeight = "1.2"
            },
            H2 = new H2Typography
            {
                FontSize = "2rem",
                FontWeight = "700",
                LineHeight = "1.25"
            },
            H3 = new H3Typography
            {
                FontSize = "1.75rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            H4 = new H4Typography
            {
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.35"
            },
            H5 = new H5Typography
            {
                FontSize = "1.25rem",
                FontWeight = "500",
                LineHeight = "1.4"
            },
            H6 = new H6Typography
            {
                FontSize = "1.125rem",
                FontWeight = "500",
                LineHeight = "1.4"
            },
            Body1 = new Body1Typography
            {
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            Body2 = new Body2Typography
            {
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            Caption = new CaptionTypography
            {
                FontSize = "0.75rem",
                FontWeight = "400",
                LineHeight = "1.4"
            },
            Button = new ButtonTypography
            {
                FontSize = "0.875rem",
                FontWeight = "500",
                LineHeight = "1.75",
                TextTransform = "uppercase"
            }
        },
        
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px",
            AppbarHeight = "64px"
        }
    };
}
