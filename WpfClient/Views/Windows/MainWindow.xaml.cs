using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfClient.Extensions;
using WpfClient.Models;
using WpfClient.ViewModels;

namespace WpfClient.Views.Windows;

public partial class MainWindow
{
    private ScrollViewer ScrollViewer => _scrollViewer ??= MessageListView.FindVisualChild<ScrollViewer>()!;
    private ScrollViewer? _scrollViewer;

    private bool _requestingNextPage;
    private Message? _pageAnchorMessage;

    public MainWindow(string token)
    {
        InitializeComponent();

        Title = "SquadTalk";
        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

        var messageCollection = (MessageCollection) MessageListView.Items.SourceCollection;

        messageCollection.MessageAdded += delegate
        {
            if (Math.Abs(ScrollViewer.ScrollableHeight - ScrollViewer.VerticalOffset) < 0.1)
            {
                ScrollViewer.ScrollToBottom();
            }
        };

        messageCollection.PageAdding += delegate
        {
            _pageAnchorMessage = FindOldestVisibleMessage(messageCollection);
        };

        messageCollection.PageAdded += delegate
        {
            if (_pageAnchorMessage is null)
            {
                ScrollViewer.ScrollToBottom();
            }
            else
            {
                MessageListView.ScrollIntoView(_pageAnchorMessage);
            }

            _requestingNextPage = false;
        };

        var viewModel = (MainViewModel) DataContext;

        Task.Run(async () =>
        {
            viewModel.SetToken(token);
            await viewModel.ConnectAsync();
        });
    }

    private Message? FindOldestVisibleMessage(MessageCollection messageCollection)
    {
        var startIndex = Math.Min(20, messageCollection.Count);

        for (var i = startIndex - 1; i >= 0; i--)
        {
            if (MessageListView.ItemContainerGenerator.ContainerFromIndex(i) is not ListViewItem {IsVisible: true} item)
            {
                continue;
            }

            var transform = item.TransformToAncestor(MessageListView);
            var rect = transform.TransformBounds(new Rect(new Point(0, 0), item.RenderSize));

            var offset = MessageListView.ActualHeight - rect.Bottom;

            if (offset > 0)
            {
                return messageCollection[i];
            }
        }

        return null;
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

    private void MessageListView_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange >= 0 || _requestingNextPage) return;

        var scrollThreshold = ScrollViewer.ScrollableHeight * 0.05;
        var offset = ScrollViewer.VerticalOffset;

        if (offset <= scrollThreshold)
        {
            var viewModel = (MainViewModel) DataContext;

            if (_requestingNextPage) return;

            _requestingNextPage = true;

            viewModel.GetNextPage();
        }
    }

    private void Titlebar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
}