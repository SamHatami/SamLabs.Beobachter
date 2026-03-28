namespace SamLabs.Beobachter.Application.Services;

public interface IThemeService
{
    AppThemeMode CurrentMode { get; }

    void SetTheme(AppThemeMode mode);
}
