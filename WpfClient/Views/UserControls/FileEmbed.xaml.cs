using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace WpfClient.Views.UserControls;

public partial class FileEmbed : UserControl
{
    public FileEmbed()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty FileTypeProperty = DependencyProperty.Register(
        nameof(FileType), typeof(FileType), typeof(FileEmbed), new PropertyMetadata(default(FileType)));
    
    public static readonly DependencyProperty DownloadUrlProperty = DependencyProperty.Register(
        nameof(DownloadUrl), typeof(string), typeof(FileEmbed), new PropertyMetadata(default(string)));
    
    public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(
        nameof(FileName), typeof(string), typeof(FileEmbed), new PropertyMetadata(default(string)));
    
    public static readonly DependencyProperty FileSizeProperty = DependencyProperty.Register(
        nameof(FileSize), typeof(long), typeof(FileEmbed), new PropertyMetadata(default(long)));

    public FileType FileType
    {
        get => (FileType) GetValue(FileTypeProperty);
        set => SetValue(FileTypeProperty, value);
    }
    
    public string DownloadUrl
    {
        get => (string) GetValue(DownloadUrlProperty);
        set => SetValue(DownloadUrlProperty, value);
    }
    
    public string FileName
    {
        get => (string) GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }
    
    public long FileSize
    {
        get => (long) GetValue(FileSizeProperty);
        set => SetValue(FileSizeProperty, value);
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        };
        
        Process.Start(processStartInfo);
        e.Handled = true;
    }
}

public enum FileType
{
    Default,
    Pdf,
    Text,
    Archive
}