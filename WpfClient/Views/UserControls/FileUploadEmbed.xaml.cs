using System.Windows;
using System.Windows.Controls;

namespace WpfClient.Views.UserControls;

public partial class FileUploadEmbed
{
    public FileUploadEmbed()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty UploadedFileNameProperty = DependencyProperty.Register(
        nameof(UploadedFileName), typeof(string), typeof(FileUploadEmbed), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty UploadedFileSizeProperty = DependencyProperty.Register(
        nameof(UploadedFileSize), typeof(long), typeof(FileUploadEmbed), new PropertyMetadata(default(long)));

    public long UploadedFileSize
    {
        get => (long) GetValue(UploadedFileSizeProperty);
        set => SetValue(UploadedFileSizeProperty, value);
    }

    public string UploadedFileName
    {
        get => (string) GetValue(UploadedFileNameProperty);
        set => SetValue(UploadedFileNameProperty, value);
    }
}