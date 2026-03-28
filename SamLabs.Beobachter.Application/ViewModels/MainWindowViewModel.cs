using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;

namespace SamLabs.Beobachter.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private string _greeting = "Beobachter foundation shell is running.";

    [ObservableProperty]
    private string _themeSummary = string.Empty;

    public MainWindowViewModel() : this(new ThemeService())
    {
    }

    public MainWindowViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseSystemTheme()
    {
        _themeService.SetTheme(AppThemeMode.System);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseLightTheme()
    {
        _themeService.SetTheme(AppThemeMode.Light);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseDarkTheme()
    {
        _themeService.SetTheme(AppThemeMode.Dark);
        UpdateThemeSummary();
    }

    private void UpdateThemeSummary()
    {
        ThemeSummary = $"Theme: {_themeService.CurrentMode}";
    }
}
