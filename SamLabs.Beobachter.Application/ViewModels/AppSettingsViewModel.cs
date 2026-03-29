using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class AppSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _channelCapacity;

    [ObservableProperty]
    private string _traceRowColor = string.Empty;

    [ObservableProperty]
    private string _traceBadgeColor = string.Empty;

    [ObservableProperty]
    private string _traceMessageColor = string.Empty;

    [ObservableProperty]
    private string _debugRowColor = string.Empty;

    [ObservableProperty]
    private string _debugBadgeColor = string.Empty;

    [ObservableProperty]
    private string _debugMessageColor = string.Empty;

    [ObservableProperty]
    private string _infoRowColor = string.Empty;

    [ObservableProperty]
    private string _infoBadgeColor = string.Empty;

    [ObservableProperty]
    private string _infoMessageColor = string.Empty;

    [ObservableProperty]
    private string _warnRowColor = string.Empty;

    [ObservableProperty]
    private string _warnBadgeColor = string.Empty;

    [ObservableProperty]
    private string _warnMessageColor = string.Empty;

    [ObservableProperty]
    private string _errorRowColor = string.Empty;

    [ObservableProperty]
    private string _errorBadgeColor = string.Empty;

    [ObservableProperty]
    private string _errorMessageColor = string.Empty;

    [ObservableProperty]
    private string _fatalRowColor = string.Empty;

    [ObservableProperty]
    private string _fatalBadgeColor = string.Empty;

    [ObservableProperty]
    private string _fatalMessageColor = string.Empty;

    public AppSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        LoadFromCurrent();
    }

    public bool Saved { get; private set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        AppSettings updated = BuildSettings();
        await _settingsService.UpdateAppSettingsAsync(updated).ConfigureAwait(false);
        Saved = true;
    }

    private void LoadFromCurrent()
    {
        AppSettings current = _settingsService.CurrentAppSettings;

        SelectedThemeIndex = current.ThemeMode switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        ChannelCapacity = current.ChannelCapacity;

        LogLevelColorOverrides c = current.LogLevelColors;
        TraceRowColor = c.Trace.Row ?? string.Empty;
        TraceBadgeColor = c.Trace.Badge ?? string.Empty;
        TraceMessageColor = c.Trace.Message ?? string.Empty;
        DebugRowColor = c.Debug.Row ?? string.Empty;
        DebugBadgeColor = c.Debug.Badge ?? string.Empty;
        DebugMessageColor = c.Debug.Message ?? string.Empty;
        InfoRowColor = c.Info.Row ?? string.Empty;
        InfoBadgeColor = c.Info.Badge ?? string.Empty;
        InfoMessageColor = c.Info.Message ?? string.Empty;
        WarnRowColor = c.Warn.Row ?? string.Empty;
        WarnBadgeColor = c.Warn.Badge ?? string.Empty;
        WarnMessageColor = c.Warn.Message ?? string.Empty;
        ErrorRowColor = c.Error.Row ?? string.Empty;
        ErrorBadgeColor = c.Error.Badge ?? string.Empty;
        ErrorMessageColor = c.Error.Message ?? string.Empty;
        FatalRowColor = c.Fatal.Row ?? string.Empty;
        FatalBadgeColor = c.Fatal.Badge ?? string.Empty;
        FatalMessageColor = c.Fatal.Message ?? string.Empty;
    }

    private AppSettings BuildSettings()
    {
        string themeMode = SelectedThemeIndex switch
        {
            1 => nameof(AppThemeMode.Light),
            2 => nameof(AppThemeMode.Dark),
            _ => nameof(AppThemeMode.System)
        };

        return new AppSettings
        {
            ThemeMode = themeMode,
            ChannelCapacity = Math.Clamp(ChannelCapacity, 1_000, 1_000_000),
            LogLevelColors = new LogLevelColorOverrides
            {
                Trace = new LogLevelColorOverride
                {
                    Row = NullIfEmpty(TraceRowColor),
                    Badge = NullIfEmpty(TraceBadgeColor),
                    Message = NullIfEmpty(TraceMessageColor)
                },
                Debug = new LogLevelColorOverride
                {
                    Row = NullIfEmpty(DebugRowColor),
                    Badge = NullIfEmpty(DebugBadgeColor),
                    Message = NullIfEmpty(DebugMessageColor)
                },
                Info = new LogLevelColorOverride
                {
                    Row = NullIfEmpty(InfoRowColor),
                    Badge = NullIfEmpty(InfoBadgeColor),
                    Message = NullIfEmpty(InfoMessageColor)
                },
                Warn = new LogLevelColorOverride
                {
                    Row = NullIfEmpty(WarnRowColor),
                    Badge = NullIfEmpty(WarnBadgeColor),
                    Message = NullIfEmpty(WarnMessageColor)
                },
                Error = new LogLevelColorOverride
                {
                    Row = NullIfEmpty(ErrorRowColor),
                    Badge = NullIfEmpty(ErrorBadgeColor),
                    Message = NullIfEmpty(ErrorMessageColor)
                },
                Fatal = new LogLevelColorOverride
                {
                    Row = NullIfEmpty(FatalRowColor),
                    Badge = NullIfEmpty(FatalBadgeColor),
                    Message = NullIfEmpty(FatalMessageColor)
                }
            }
        };
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
