using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SamLabs.Beobachter.Application.Views;

public partial class AppSettingsWindow : Window
{
    public AppSettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
