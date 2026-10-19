using System.Numerics;
using Microsoft.UI.Composition;
using Windows.UI;

namespace Flowery.Composition;

/// <summary>A retained native vector whose resources belong to one visual scope.</summary>
internal sealed class CompositionVectorVisual
{
    internal CompositionVectorVisual(CompositionVisualScope scope, CompositionGeometry geometry, Color fill)
    {
        Geometry = scope.Track(geometry);
        Visual = scope.Track(scope.Compositor.CreateShapeVisual());
        Shape = scope.Track(scope.Compositor.CreateSpriteShape(geometry));
        Fill = scope.Track(scope.Compositor.CreateColorBrush(fill));
        Shape.FillBrush = Fill;
        Visual.Shapes.Add(Shape);
    }

    internal CompositionGeometry Geometry { get; }
    internal ShapeVisual Visual { get; }
    internal CompositionSpriteShape Shape { get; }
    internal CompositionColorBrush Fill { get; }

    internal void SetSize(Vector2 size)
    {
        if (Visual.Size != size)
            Visual.Size = size;
    }
}
