using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace WpfClient.Views.UserControls;

public partial class GifEmbed : UserControl
{
    public GifEmbed()
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

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(ImageSource);
            image.EndInit();

            ImageBehavior.SetAnimatedSource(Image, image);
        }
    }
}