using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfClient.Models;
using WpfClient.ViewModels;

namespace WpfClient.Views.Windows;

public partial class MainWindow
{
    public MainWindow(string token)
    {
        InitializeComponent();
        Title = "SquadTalk";

        ((ObservableCollection<Message>) MessageListView.Items.SourceCollection).CollectionChanged += delegate
        {
            MessageListView.ScrollIntoView(MessageListView.Items[^1]);
        };

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SetToken(token);
            Task.Run(viewModel.ConnectAsync);
        }
    }

    private void Titlebar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}