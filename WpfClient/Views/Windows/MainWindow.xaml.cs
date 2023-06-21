using System;
using System.Collections.Generic;
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

    private double _scrollPosition;

    private bool _requestedNextPage;

    public MainWindow(string token)
    {
        InitializeComponent();
        
        Title = "SquadTalk";
        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

        var messageCollection = (MessageCollection) MessageListView.Items.SourceCollection;

        messageCollection.MessageAdded += delegate(MessageCollection _, Message message)
        {
            var scrollableHeight = ScrollViewer.ScrollableHeight;
            var verticalOffset = ScrollViewer.VerticalOffset;
            
            if (Math.Abs(scrollableHeight - verticalOffset) < 0.1)
            {
                MessageListView.ScrollIntoView(message);
            }
        };

        messageCollection.PageAdding += delegate
        {
            _scrollPosition = ScrollViewer.ScrollableHeight - ScrollViewer.VerticalOffset;
        };
        
        messageCollection.PageAdded += delegate(MessageCollection sender, IList<Message> page)
        {
            _requestedNextPage = false;

            if (sender.Count == page.Count)
            {
                MessageListView.ScrollIntoView(page[^1]);
            }
            else
            {
                ScrollViewer.UpdateLayout();
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ScrollableHeight - _scrollPosition);
            }
        };

        var viewModel = (MainViewModel) DataContext;

        Task.Run(async () =>
        {
            viewModel.SetToken(token);
            await viewModel.ConnectAsync();
        });
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
        if (e.VerticalChange >= 0 || _requestedNextPage) return;
        
        var scrollThreshold = ScrollViewer.ScrollableHeight * 0.2;
        var offset = ScrollViewer.VerticalOffset;

        if (offset <= scrollThreshold)
        {
            var viewModel = (MainViewModel) DataContext;
            
            if (_requestedNextPage) return;
            
            _requestedNextPage = true;
            
            viewModel.GetNextPage();
        }
    }

    private void Titlebar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
}