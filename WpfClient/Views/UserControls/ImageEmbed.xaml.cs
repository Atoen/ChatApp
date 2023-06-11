using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfClient.Views.UserControls;

public partial class ImageEmbed : UserControl
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

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(ImageSource);
            bitmap.EndInit();

            Image.Source = bitmap;
        }
    }
}