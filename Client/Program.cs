using System.Drawing;
using ConsoleGUI;
using ConsoleGUI.ConsoleDisplay;
using ConsoleGUI.UI.Events;
using ConsoleGUI.UI.New;

Application.Start();
Input.TreatControlCAsInput = true;

var label = new Label
{
    Text = new RichText("That's"),
    Position = (5, 5),
    ResizeMode = ResizeMode.Stretch,
    ColorTheme = Color.Blue,
    MaxSize = (20, 3)
};

var text = label.Text as RichText;

text!.AppendRich(" rich", Color.Gold);

text.RichData[^3].Background = Color.Red;
text.RichData[5].TextMode = TextMode.Underline;

label.MouseLeftDown += delegate(Control sender, MouseEventArgs eventArgs)
{
    if (sender is not Label {Text: RichText richText}) return;

    richText.Content += " fr";
};
