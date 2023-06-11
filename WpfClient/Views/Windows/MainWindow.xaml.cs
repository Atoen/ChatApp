using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Controls;
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

        ((ObservableCollection<Message>) MessageListView.Items.SourceCollection).CollectionChanged += delegate(object? _, NotifyCollectionChangedEventArgs args)
        {
            if (args is {Action: NotifyCollectionChangedAction.Add, NewItems: [.., var last]})
            {
                MessageListView.ScrollIntoView(last!);
            }
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