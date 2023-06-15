using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
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
        
        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

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

    private async void MessageBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.V || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

        if (!Clipboard.ContainsImage()) return;
        
        var command = ((MainViewModel) DataContext).PasteImageCommand;

        if (command.CanExecute(null))
        {
            await command.ExecuteAsync(null);
        }
    }
}