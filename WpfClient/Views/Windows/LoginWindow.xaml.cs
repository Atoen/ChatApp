using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Konscious.Security.Cryptography;
using WpfClient.ViewModels;

namespace WpfClient.Views.Windows;

public partial class LoginWindow : Window
{
    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginWindowViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
    
    public LoginWindow()
    {
        InitializeComponent();
        TitleBar.MaximizeButton.IsEnabled = false;
    }

    private void Titlebar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}