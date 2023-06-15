using System;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfClient.Views.Windows;

namespace WpfClient.Views.UserControls;

public partial class ImageEmbed
{
    public ImageEmbed()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
        nameof(ImageSource), typeof(string), typeof(ImageEmbed), new PropertyMetadata(default(string)));

    public string ImageSource
    {
        get => (string) GetValue(ImageSourceProperty);
        set
        {
            SetValue(ImageSourceProperty, value);
            CreateImage();
        }
    }

    private void CreateImage()
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(ImageSource);
        bitmap.EndInit();

        Image.Source = bitmap;
    }

    private void Image_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        new ImageOverlayWindow(ImageSource, Window.GetWindow(this)!).Show();
    }

    private void Image_OnLoaded(object sender, RoutedEventArgs e)
    {
        Image.SizeChanged += ImageOnSizeChanged;
    }

    private void ImageOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Image {Source: BitmapImage source} image) return;

        if (source is {PixelHeight: > 1, PixelWidth: > 1})
        {
            image.SizeChanged -= ImageOnSizeChanged;
            image.MaxWidth = Math.Max(200, source.PixelWidth);
            image.MaxHeight = Math.Max(200, source.PixelHeight);
        }
    }
}