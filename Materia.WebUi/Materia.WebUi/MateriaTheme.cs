using MudBlazor;

namespace Materia.WebUi;

public static class MateriaTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary          = "#3B6FD4",
            PrimaryDarken    = "#2A56B0",
            PrimaryLighten   = "#6B96E8",
            Secondary        = "#64748B",
            Background       = "#F1F5F9",
            Surface          = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText       = "#1E293B",
            DrawerBackground = "#1E293B",
            DrawerText       = "#94A3B8",
            DrawerIcon       = "#94A3B8",
            TextPrimary      = "#1E293B",
            TextSecondary    = "#64748B",
            ActionDefault    = "#64748B",
            Divider          = "#E2E8F0",
            DividerLight     = "#F1F5F9",
            Success          = "#16A34A",
            Error            = "#DC2626",
            Warning          = "#D97706",
            Info             = "#0284C7",
            TableHover       = "rgba(59,111,212,0.05)",
            TableStriped     = "rgba(0,0,0,0.018)",
        },
        PaletteDark = new PaletteDark
        {
            Primary          = "#6B96E8",
            PrimaryDarken    = "#4A78D4",
            Background       = "#0F172A",
            Surface          = "#1E293B",
            AppbarBackground = "#1E293B",
            AppbarText       = "#E2E8F0",
            DrawerBackground = "#0F172A",
            DrawerText       = "#94A3B8",
            DrawerIcon       = "#94A3B8",
            TextPrimary      = "#E2E8F0",
            TextSecondary    = "#94A3B8",
            ActionDefault    = "#94A3B8",
            Divider          = "#334155",
            DividerLight     = "#1E293B",
            Success          = "#22C55E",
            Error            = "#EF4444",
            Warning          = "#F59E0B",
            Info             = "#38BDF8",
            TableHover       = "rgba(107,150,232,0.07)",
        },
    };
}
