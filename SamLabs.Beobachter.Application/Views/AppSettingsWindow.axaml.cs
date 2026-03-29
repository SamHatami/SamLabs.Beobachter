using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using SamLabs.Beobachter.Application.ViewModels;

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

    private void OnColorSwatchClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string propertyName)
        {
            return;
        }

        if (DataContext is not AppSettingsViewModel vm)
        {
            return;
        }

        PropertyInfo? prop = typeof(AppSettingsViewModel).GetProperty(propertyName);
        if (prop is null)
        {
            return;
        }

        Color? currentColor = prop.GetValue(vm) as Color?;

        var colorView = new ColorView
        {
            Color = currentColor ?? Colors.Gray
        };

        var applyButton = new Button
        {
            Content = "Apply",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };

        var flyout = new Flyout
        {
            Content = new StackPanel
            {
                Spacing = 4,
                Children = { colorView, applyButton }
            },
            Placement = PlacementMode.BottomEdgeAlignedLeft
        };

        applyButton.Click += (_, _) =>
        {
            prop.SetValue(vm, (Color?)colorView.Color);
            flyout.Hide();
        };

        flyout.ShowAt(button);
    }
}
