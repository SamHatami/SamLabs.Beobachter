using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class LogLevelColorItemViewModel : ViewModelBase
{
    public LogLevelColorItemViewModel(string levelName)
    {
        LevelName = levelName;
    }

    public string LevelName { get; }

    [ObservableProperty]
    private Color _rowColor = Colors.Transparent;

    [ObservableProperty]
    private bool _hasRowColor;

    [ObservableProperty]
    private Color _badgeColor = Colors.Transparent;

    [ObservableProperty]
    private bool _hasBadgeColor;

    [ObservableProperty]
    private Color _messageColor = Colors.Transparent;

    [ObservableProperty]
    private bool _hasMessageColor;

    [RelayCommand]
    private void ClearRowColor()
    {
        RowColor = Colors.Transparent;
        HasRowColor = false;
    }

    [RelayCommand]
    private void ClearBadgeColor()
    {
        BadgeColor = Colors.Transparent;
        HasBadgeColor = false;
    }

    [RelayCommand]
    private void ClearMessageColor()
    {
        MessageColor = Colors.Transparent;
        HasMessageColor = false;
    }

    partial void OnRowColorChanged(Color value)
    {
        HasRowColor = value != Colors.Transparent;
    }

    partial void OnBadgeColorChanged(Color value)
    {
        HasBadgeColor = value != Colors.Transparent;
    }

    partial void OnMessageColorChanged(Color value)
    {
        HasMessageColor = value != Colors.Transparent;
    }

    public void LoadFrom(LogLevelColorOverride source)
    {
        RowColor = ParseColor(source.Row) ?? Colors.Transparent;
        BadgeColor = ParseColor(source.Badge) ?? Colors.Transparent;
        MessageColor = ParseColor(source.Message) ?? Colors.Transparent;
    }

    public LogLevelColorOverride ToOverride()
    {
        return new LogLevelColorOverride
        {
            Row = HasRowColor ? ToHex(RowColor) : null,
            Badge = HasBadgeColor ? ToHex(BadgeColor) : null,
            Message = HasMessageColor ? ToHex(MessageColor) : null
        };
    }

    private static Color? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        return Color.TryParse(hex.Trim(), out var c) ? c : null;
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
