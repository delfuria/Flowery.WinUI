using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace Flowery.Composition;

/// <summary>Owns one attached visual tree and its native resources on the UI thread.</summary>
internal sealed class CompositionVisualScope : IDisposable
{
    private readonly FrameworkElement _host;
    private readonly XamlRoot _xamlRoot;
    private readonly List<IDisposable> _resources = [];
    private bool _disposed;

    private CompositionVisualScope(FrameworkElement host)
    {
        _host = host;
        _xamlRoot = host.XamlRoot;
        Compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
        Root = Track(Compositor.CreateContainerVisual());
    }

    internal Compositor Compositor { get; }
    internal ContainerVisual Root { get; }

    internal bool IsAttached => !_disposed && CompositionVisualTree.GetConnectedRoot(_host) == _xamlRoot &&
        ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(_host), Root);

    internal static CompositionVisualScope? TryAttach(FrameworkElement host)
    {
        if (CompositionVisualTree.GetConnectedRoot(host) is null ||
            ElementCompositionPreview.GetElementChildVisual(host) is not null)
            return null;

        var scope = new CompositionVisualScope(host);
        try
        {
            ElementCompositionPreview.SetElementChildVisual(host, scope.Root);
            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    internal T Track<T>(T resource) where T : IDisposable
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _resources.Add(resource);
        return resource;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(_host), Root))
            ElementCompositionPreview.SetElementChildVisual(_host, null);
        Root.Children.RemoveAll();
        for (var index = _resources.Count - 1; index >= 0; index--)
            _resources[index].Dispose();
        _resources.Clear();
    }
}
