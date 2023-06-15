using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WpfClient.Views.Windows;

public partial class ImageOverlayWindow
{
    private readonly string _imageSource;
    private bool _closed;

    public ImageOverlayWindow(string imageSource, Window parent)
    {
        _imageSource = imageSource;
        InitializeComponent();

        var bitmap = new BitmapImage(new Uri(imageSource));
        Image.Source = bitmap;
        
        Width = parent.ActualWidth;
        Height = parent.ActualHeight;
        Left = parent.Left;
        Top = parent.Top;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        
        if (_closed) return;
        
        _closed = true;
        Close();
    }

    private void DownloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _imageSource,
            UseShellExecute = true
        };

        Process.Start(processStartInfo);
        e.Handled = true;
    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_closed) return;
        
        _closed = true;
        Close();
    }
}