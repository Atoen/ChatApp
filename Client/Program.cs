using System.Diagnostics;
using System.Drawing;
using ConsoleGUI;
using ConsoleGUI.UI.Widgets;
using ConsoleGUI.Visuals;
// using ConsoleGUI.UI.Old.Widgets;
using ConsoleGUI.Visuals.Figlet;
using Entry = ConsoleGUI.UI.Widgets.Entry;

Application.Start();

var grid = new Grid
{
    // Size = (10, 10),
    Columns = {new Column(12), new Column(10)},
    Rows =
    {
        new Row(4),
        new Row(12)
    },
    // MinSize = (10, 10),
    // MaxSize = (10, 10),
    
    FillScreen = true,
    Lines = {Visible = true, Color = Color.Black, Style = GridLineStyle.SingleBold}
};







