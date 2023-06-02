using System.Windows;
using System.Windows.Input;

namespace WpfClient.Views.UserControls;

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
            window.BorderThickness = new Thickness(0);
        }
        else
        {
            window.WindowState = WindowState.Maximized;
            MaximizeButton.Content = "🗗";
            window.BorderThickness = new Thickness(8);
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

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && MaximizeButton.IsEnabled)
        {
            MaximizeButton_OnClick(sender, e);
        }
    }
}