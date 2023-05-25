using System.Windows;

namespace WpfClient.View.UserControls;

public partial class TitleBar
{
    public TitleBar()
    {
        InitializeComponent();
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this)!;

        if (window.WindowState == WindowState.Maximized)
        {
            MaximizeButton.Content = "🗖";
            window.WindowState = WindowState.Normal;
        }
        else
        {
            window.WindowState = WindowState.Maximized;
            MaximizeButton.Content = "🗗";
        }
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.Close();
    }
}