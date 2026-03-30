using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SamLabs.Beobachter.Application.ViewModels;

namespace SamLabs.Beobachter.Application.Views;

public partial class ReceiverSetupWindow : Window
{
    public ReceiverSetupWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReceiverSetupViewModel receiverSetup)
        {
            Close();
            return;
        }

        await receiverSetup.SaveReceiverSetupCommand.ExecuteAsync(null);
        if (!receiverSetup.ReceiverSetupStatus.StartsWith("Validation failed", StringComparison.OrdinalIgnoreCase))
        {
            Close();
        }
    }
}
