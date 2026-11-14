using Beutl.Composition;
using Beutl.Engine;
using Beutl.Graphics;
using Beutl.Media;
using Beutl.ProjectSystem;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The preview handle that moves the centre of a drawable that covers the whole screen.</summary>
internal static class CenterHandle
{
    public const string Id = "center";

    /// <param name="drawable">The drawable, which draws in the coordinates of the screen.</param>
    /// <param name="center">Its centre, measured from the middle of the screen.</param>
    /// <param name="time">The time the preview shows.</param>
    public static IReadOnlyList<EditingHandle> Get(Drawable drawable, IExpressionProperty<Point> center, MediaTime time)
    {
        if (drawable.FindHierarchicalParent<Scene>() is not { } scene)
            return [];

        PixelSize frame = scene.FrameSize.Value;
        Point offset = new CompositionContext(time).Get(center);
        var position = new Point(frame.Width / 2f + offset.X, frame.Height / 2f + offset.Y);
        // The host moves the property by the drag, on its keyframes when it is animated.
        return [new EditingHandle(Id, position, "中心") { Property = center }];
    }
}
