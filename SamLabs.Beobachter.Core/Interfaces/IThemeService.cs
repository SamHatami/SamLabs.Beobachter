using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface IThemeService
{
    AppThemeMode CurrentMode { get; }

    void SetTheme(AppThemeMode mode);
}
