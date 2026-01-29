using MudBlazor;

namespace GolfTrackerApp.Web.Theme;

/// <summary>
/// Custom MudBlazor theme for the Golf Tracker App.
/// This centralizes all color and typography settings for the application.
/// </summary>
public static class GolfTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            // Primary Golf Green
            Primary = "#3d7c3f",
            PrimaryContrastText = "#ffffff",
            PrimaryDarken = "#2c5530",
            PrimaryLighten = "#5e9c61",
            
            // Secondary Lime Green
            Secondary = "#8bc34a",
            SecondaryContrastText = "#ffffff",
            SecondaryDarken = "#5a7b2a",
            SecondaryLighten = "#a5d76a",
            
            // Tertiary (accent)
            Tertiary = "#ff9800",
            TertiaryContrastText = "#ffffff",
            
            // Status Colors
            Success = "#4caf50",
            SuccessContrastText = "#ffffff",
            Warning = "#ff9800",
            WarningContrastText = "#ffffff",
            Error = "#f44336",
            ErrorContrastText = "#ffffff",
            Info = "#2196f3",
            InfoContrastText = "#ffffff",
            
            // Backgrounds
            Background = "#fafafa",
            BackgroundGray = "#f5f5f5",
            Surface = "#ffffff",
            
            // Text
            TextPrimary = "#424242",
            TextSecondary = "#666666",
            TextDisabled = "#999999",
            
            // App Bar
            AppbarBackground = "#3d7c3f",
            AppbarText = "#ffffff",
            
            // Drawer
            DrawerBackground = "#ffffff",
            DrawerText = "#424242",
            DrawerIcon = "#3d7c3f",
            
            // Dividers and Lines
            Divider = "#e0e0e0",
            DividerLight = "#eeeeee",
            
            // Action Colors
            ActionDefault = "#757575",
            ActionDisabled = "#bdbdbd",
            ActionDisabledBackground = "#e0e0e0",
            
            // Table
            TableHover = "#f5f5f5",
            TableStriped = "#fafafa",
            
            // Overlay
            OverlayDark = "rgba(0,0,0,0.5)",
            OverlayLight = "rgba(255,255,255,0.5)"
        },
        
        PaletteDark = new PaletteDark
        {
            // Primary Golf Green (lighter for dark mode)
            Primary = "#5e9c61",
            PrimaryContrastText = "#ffffff",
            PrimaryDarken = "#3d7c3f",
            PrimaryLighten = "#81c784",
            
            // Secondary
            Secondary = "#aed581",
            SecondaryContrastText = "#212121",
            
            // Tertiary
            Tertiary = "#ffb74d",
            TertiaryContrastText = "#212121",
            
            // Status Colors
            Success = "#81c784",
            Warning = "#ffb74d",
            Error = "#e57373",
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
