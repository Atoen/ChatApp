using System.Drawing;
using ConsoleGUI;
using ConsoleGUI.ConsoleDisplay;
using ConsoleGUI.UI.Events;
using ConsoleGUI.UI.New;
using ConsoleGUI.UI.Widgets;
using ConsoleGUI.Visuals.Figlet;
using BigText = ConsoleGUI.UI.BigText;
using Button = ConsoleGUI.UI.Widgets.Button;
using Entry = ConsoleGUI.UI.Widgets.Entry;
using Label = ConsoleGUI.UI.Widgets.Label;

namespace Client;

public class MainView
{
    private readonly Entry _usernameEntry;
    private readonly TextBox _receivedMessagesBox;
    private readonly TextBox _inputBox;
    private readonly Button _sendButton;
    private readonly Button _connectButton;

    private readonly Server.Client _client = new();

    public MainView()
    {
        Application.ApplicationExit += delegate { _client.Close(); };

        var mainGrid = new Grid
        {
            Color = Color.MidnightBlue,
            ShowGridLines = true,
            ResizeMode = ResizeMode.Expand
        };

        mainGrid.FitToScreen();

        mainGrid.Columns.Add(new Column());
        mainGrid.Columns.Add(new Column(15));
        mainGrid.Rows.Add(new Row());
        mainGrid.Rows.Add(new Row());
        mainGrid.Rows.Add(new Row());

        var titleLabel = new Label
        {
            Text = new BigText("chat", Font.CalvinS) {Foreground = Color.Wheat},
            Color = Color.Empty
        };
        mainGrid.SetColumnAndRow(titleLabel, 0, 0);

        _usernameEntry = new Entry
        {
            InputMode = AllowedSymbols.Alphanumeric,
            MaxTextLenght = 12,
            Text = { String = "Username" }
        };
        mainGrid.SetColumnAndRow(_usernameEntry, 0, 1);

        _connectButton = new Button
        {
            Text = { String = "Connect" },
            OnClick = Connect
        };
        mainGrid.SetColumnAndRow(_connectButton, 0, 2);

        _receivedMessagesBox = new TextBox
        {
            DisplayWatermark = false,
            MaxLineLength = 40,
            Text = { Alignment = Alignment.Left },
            Color = Color.DimGray,
            ReadOnly = true,
            ResizeMode = ResizeMode.Expand
        };
        mainGrid.SetColumnAndRow(_receivedMessagesBox, 1, 0);
        mainGrid.SetColumnSpanAndRowSpan(_receivedMessagesBox, 1, 2);

        var inputGrid = new Grid
        {
            Color = Color.Black,
            InnerPadding = (1, 0),
            ResizeMode = ResizeMode.Expand,
            ShowGridLines = true
        };

        inputGrid.Columns.Add(new Column());
        inputGrid.Columns.Add(new Column(10));
        inputGrid.Rows.Add(new Row());

        _inputBox = new TextBox
        {
            Watermark = { String = "Send...", Foreground = Color.Red},
            MinSize = (30, 1),
            MaxTextLength = 20,
            MaxLineLength = 10,
            Text = { Alignment = Alignment.Left },
            Color = Color.DimGray
        };
        inputGrid.SetColumnAndRow(_inputBox, 0, 0);

        _sendButton = new Button
        {
            Text = { String = "Send", Foreground = Color.Orange, TextMode = TextMode.Bold },
            OnClick = Send
        };
        inputGrid.SetColumnAndRow(_sendButton, 1, 0);

        mainGrid.SetColumnAndRow(inputGrid, 1, 2);

        inputGrid.DoubleClick += (_, _) => { };
    }

    private async void Connect()
    {
        await _client.ConnectToServerAsync(_usernameEntry.Text.String);
    }

    private async void Send()
    {
        var message = _inputBox.Text.String;

        if (message == string.Empty) return;

        await _client.SendMessageAsync(message);
    }
}