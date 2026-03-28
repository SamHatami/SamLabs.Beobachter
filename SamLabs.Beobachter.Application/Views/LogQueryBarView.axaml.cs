using Avalonia.Controls;

namespace SamLabs.Beobachter.Application.Views;

public partial class LogQueryBarView : UserControl
{
    public LogQueryBarView()
    {
        InitializeComponent();
    }

    public void FocusSearchBox()
    {
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }
}
