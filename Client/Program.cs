using System.Drawing;
using ConsoleGUI;
using ConsoleGUI.UI.New;
using ConsoleGUI.Visuals.Figlet;

Application.Start();
Input.TreatControlCAsInput = true;

var label = new Button<Text>
{
    Text = { Foreground = Color.Orange, Content = "o"},
    Position = (5, 5),
    ResizeMode = ResizeMode.Grow,
    ColorTheme = Color.RoyalBlue
};

label.OnClick = () =>
{
    label.Text.Content += " oro";
    // label.Text.Font = Font.CalvinS;
};

label.MouseRightDown += (sender, eventArgs) =>
{
    label.Text.Content = "01";
};
