using Avalonia.Controls;
using Avalonia.Interactivity;
using SamLabs.Beobachter.Application.ViewModels;

namespace SamLabs.Beobachter.Application.Views;

public partial class ReceiverTreeView : UserControl
{
    public ReceiverTreeView()
    {
        InitializeComponent();
    }

    private void OnOpenEditorClick(object? sender, RoutedEventArgs e)
    {
        (DataContext as ReceiverTreeViewModel)?.RequestOpenEditor();
    }

    private void OnEditReceiverClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.DataContext is ReceiverDefinitionViewModel receiver &&
            DataContext is ReceiverTreeViewModel vm)
        {
            vm.ReceiverSetup.SelectedReceiverDefinition = receiver;
            vm.RequestOpenEditor();
        }
    }
}
