using System;
using System.Windows;
using System.Windows.Input;
using WpfClient.Views.Windows;
using XamlAnimatedGif;

namespace WpfClient.Views.UserControls;

public partial class GifEmbed
{
    public GifEmbed()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty GifSourceProperty = DependencyProperty.Register(
        nameof(GifSource), typeof(string), typeof(ImageEmbed), new PropertyMetadata(default(string)));

    public string GifSource
    {
        get => (string) GetValue(GifSourceProperty);
        set
        {
            SetValue(GifSourceProperty, value);
            AnimationBehavior.SetSourceUri(Image, new Uri(GifSource));
        }
    }

    private void Gif_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        new ImageOverlayWindow(GifSource, Window.GetWindow(this)!).Show();
    }
}