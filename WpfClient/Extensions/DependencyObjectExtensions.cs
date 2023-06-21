using System.Windows;
using System.Windows.Media;

namespace WpfClient.Extensions;

public static class DependencyObjectExtensions
{
    public static T? FindVisualChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var foundChild = FindVisualChild<T>(child);
            if (foundChild is not null)
            {
                return foundChild;
            }
        }

        return null;
    }
}