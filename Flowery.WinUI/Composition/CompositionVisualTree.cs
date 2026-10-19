using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Composition;

internal static class CompositionVisualTree
{
    // IsLoaded can be false for connected native template elements. A cached XamlRoot
    // alone is also insufficient: detached template trees can retain that reference.
    internal static XamlRoot? GetConnectedRoot(FrameworkElement element)
    {
        if (element.XamlRoot is not { } root)
            return null;

        if (Contains(root.Content, element))
            return root;

        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
        {
            if (popup.IsOpen && Contains(popup.Child, element))
                return root;
        }
        return null;
    }

    private static bool Contains(DependencyObject? root, DependencyObject element)
    {
        if (root is null)
            return false;
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current.Equals(root))
                return true;
        }
        return false;
    }
}
