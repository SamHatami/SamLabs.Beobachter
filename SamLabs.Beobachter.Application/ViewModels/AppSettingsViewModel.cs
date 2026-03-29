using System;
using System.Threading.Tasks;
using Avalonia.Media;
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

    [ObservableProperty] private Color? _traceRowColor;
    [ObservableProperty] private Color? _traceBadgeColor;
    [ObservableProperty] private Color? _traceMessageColor;

    [ObservableProperty] private Color? _debugRowColor;
    [ObservableProperty] private Color? _debugBadgeColor;
    [ObservableProperty] private Color? _debugMessageColor;

    [ObservableProperty] private Color? _infoRowColor;
    [ObservableProperty] private Color? _infoBadgeColor;
    [ObservableProperty] private Color? _infoMessageColor;

    [ObservableProperty] private Color? _warnRowColor;
    [ObservableProperty] private Color? _warnBadgeColor;
    [ObservableProperty] private Color? _warnMessageColor;

    [ObservableProperty] private Color? _errorRowColor;
    [ObservableProperty] private Color? _errorBadgeColor;
    [ObservableProperty] private Color? _errorMessageColor;

    [ObservableProperty] private Color? _fatalRowColor;
    [ObservableProperty] private Color? _fatalBadgeColor;
    [ObservableProperty] private Color? _fatalMessageColor;

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

    [RelayCommand]
    private void ClearColor(string parameterName)
    {
        switch (parameterName)
        {
            case nameof(TraceRowColor): TraceRowColor = null; break;
            case nameof(TraceBadgeColor): TraceBadgeColor = null; break;
            case nameof(TraceMessageColor): TraceMessageColor = null; break;
            case nameof(DebugRowColor): DebugRowColor = null; break;
            case nameof(DebugBadgeColor): DebugBadgeColor = null; break;
            case nameof(DebugMessageColor): DebugMessageColor = null; break;
            case nameof(InfoRowColor): InfoRowColor = null; break;
            case nameof(InfoBadgeColor): InfoBadgeColor = null; break;
            case nameof(InfoMessageColor): InfoMessageColor = null; break;
            case nameof(WarnRowColor): WarnRowColor = null; break;
            case nameof(WarnBadgeColor): WarnBadgeColor = null; break;
            case nameof(WarnMessageColor): WarnMessageColor = null; break;
            case nameof(ErrorRowColor): ErrorRowColor = null; break;
            case nameof(ErrorBadgeColor): ErrorBadgeColor = null; break;
            case nameof(ErrorMessageColor): ErrorMessageColor = null; break;
            case nameof(FatalRowColor): FatalRowColor = null; break;
            case nameof(FatalBadgeColor): FatalBadgeColor = null; break;
            case nameof(FatalMessageColor): FatalMessageColor = null; break;
        }
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
        TraceRowColor = ParseColor(c.Trace.Row);
        TraceBadgeColor = ParseColor(c.Trace.Badge);
        TraceMessageColor = ParseColor(c.Trace.Message);
        DebugRowColor = ParseColor(c.Debug.Row);
        DebugBadgeColor = ParseColor(c.Debug.Badge);
        DebugMessageColor = ParseColor(c.Debug.Message);
        InfoRowColor = ParseColor(c.Info.Row);
        InfoBadgeColor = ParseColor(c.Info.Badge);
        InfoMessageColor = ParseColor(c.Info.Message);
        WarnRowColor = ParseColor(c.Warn.Row);
        WarnBadgeColor = ParseColor(c.Warn.Badge);
        WarnMessageColor = ParseColor(c.Warn.Message);
        ErrorRowColor = ParseColor(c.Error.Row);
        ErrorBadgeColor = ParseColor(c.Error.Badge);
        ErrorMessageColor = ParseColor(c.Error.Message);
        FatalRowColor = ParseColor(c.Fatal.Row);
        FatalBadgeColor = ParseColor(c.Fatal.Badge);
        FatalMessageColor = ParseColor(c.Fatal.Message);
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
                    Row = ToHex(TraceRowColor),
                    Badge = ToHex(TraceBadgeColor),
                    Message = ToHex(TraceMessageColor)
                },
                Debug = new LogLevelColorOverride
                {
                    Row = ToHex(DebugRowColor),
                    Badge = ToHex(DebugBadgeColor),
                    Message = ToHex(DebugMessageColor)
                },
                Info = new LogLevelColorOverride
                {
                    Row = ToHex(InfoRowColor),
                    Badge = ToHex(InfoBadgeColor),
                    Message = ToHex(InfoMessageColor)
                },
                Warn = new LogLevelColorOverride
                {
                    Row = ToHex(WarnRowColor),
                    Badge = ToHex(WarnBadgeColor),
                    Message = ToHex(WarnMessageColor)
                },
                Error = new LogLevelColorOverride
                {
                    Row = ToHex(ErrorRowColor),
                    Badge = ToHex(ErrorBadgeColor),
                    Message = ToHex(ErrorMessageColor)
                },
                Fatal = new LogLevelColorOverride
                {
                    Row = ToHex(FatalRowColor),
                    Badge = ToHex(FatalBadgeColor),
                    Message = ToHex(FatalMessageColor)
                }
            }
        };
    }

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        return Color.TryParse(hex.Trim(), out var color) ? color : null;
    }

    private static string? ToHex(Color? color)
    {
        return color.HasValue ? color.Value.ToString() : null;
    }
}
