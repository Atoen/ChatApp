using System.Threading.Tasks;
using System.Windows.Input;
using WpfClient.ViewModels;

namespace WpfClient.Views.Windows;

public partial class MainWindow
{
    public MainWindow(string token)
    {
        InitializeComponent();
        Title = "SquadTalk";

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