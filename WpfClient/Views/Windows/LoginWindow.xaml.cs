using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Konscious.Security.Cryptography;

namespace WpfClient.Views.Windows;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        TitleBar.MaximizeButton.IsEnabled = false;
    }

    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void LoginButton_OnClick(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;
        
        var username = UsernameBox.Text;
        var password = PasswordBox.Password;
        
        if (string.IsNullOrWhiteSpace(username))
        {
            ErrorTextBlock.Text = "Username must not be empty";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorTextBlock.Text = "Password must not be empty";
            return;
        }
        
        var hashedPassword = HashPassword(password, username);

        ErrorTextBlock.Text = "Incorrect username or password";
        
        // new MainWindow().Show();
        // Close();
    }

    private void SignupButton_OnClick(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var username = UsernameBox.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            ErrorTextBlock.Text = "Username must not be empty";
            return;
        }

        if (username.Length is < 2 or > 32)
        {
            ErrorTextBlock.Text = "Username must be between 2 and 32 characters long";
            return;
        }
        
        if (username == "User1")
        {
            ErrorTextBlock.Text = "Username is already taken";
            return;
        }
        
        var password = PasswordBox.Password;

        if (password.Length is < 3 or > 64)
        {
            ErrorTextBlock.Text = "Password must be between 3 and 64 characters long";
            return;
        }

        var hashedPassword = HashPassword(password, username);
        
        ErrorTextBlock.Text = "User already exists";
    }

    private static string HashPassword(string password, string username)
    {
        // salting on server side
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
        argon2.DegreeOfParallelism = 8;
        argon2.MemorySize = 8192;
        argon2.Iterations = 8;
        argon2.AssociatedData = Encoding.UTF8.GetBytes(username);

        var hash = argon2.GetBytes(64);
        return Convert.ToBase64String(hash);
    }
}