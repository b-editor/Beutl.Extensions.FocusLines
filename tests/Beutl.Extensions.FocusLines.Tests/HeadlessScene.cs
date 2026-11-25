using Beutl.Composition;
using Beutl.Engine;
using Beutl.Graphics;
using Beutl.Graphics.Backend;
using Beutl.Graphics.Rendering;
using Beutl.Graphics.Shapes;
using Beutl.Media;
using Beutl.ProjectSystem;

namespace Beutl.Extensions.FocusLines.Tests;

/// <summary>Renders a scene through Beutl's own pipeline without starting the editor.</summary>
internal static class HeadlessScene
{
    private static readonly Lazy<bool> s_isAvailable = new(() =>
        RenderThread.Dispatcher.Invoke(() => GraphicsContextFactory.GetOrCreateShared() is not null));

    public static void EnsureAvailable()
    {
        if (!s_isAvailable.Value)
            Assert.Ignore("No graphics context is available on this host.");
    }

    /// <summary>A 1920x1080 scene with each object on its own layer, the first at the bottom.</summary>
    public static Scene Create(params EngineObject[] objects) => Create(1920, 1080, objects);

    public static Scene Create(int width, int height, params EngineObject[] objects)
    {
        var scene = new Scene(width, height, "HeadlessScene");
        int layer = 0;
        foreach (EngineObject obj in objects)
        {
            var element = new Element
            {
                Start = { Value = MediaTime.Zero },
                Length = { Value = MediaTime.FromSeconds(10) },
                ZIndex = { Value = layer++ },
            };
            element.AddObject(obj);
            scene.AddChild(element);
        }

        return scene;
    }

    /// <summary>A rectangle that fills a 1920x1080 screen, to draw the lines over.</summary>
    public static RectShape Background(Color color)
    {
        var rect = new RectShape();
        rect.Width.Value = 1920;
        rect.Height.Value = 1080;
        rect.Fill.Value = new SolidColorBrush(color);
        return rect;
    }

    /// <summary>Renders one frame and returns it as 8-bit sRGB BGRA.</summary>
    public static Bitmap Render(Scene scene, TimeSpan time)
    {
        EnsureAvailable();
        return RenderThread.Dispatcher.Invoke(() =>
        {
            // The scenes hold no text or media, so they need no fonts or decoders.
            using var renderer = new SceneRenderer(scene, SceneRenderServices.Empty, RenderIntent.Delivery);
            CompositionFrame frame = renderer.Compositor.EvaluateGraphics(MediaTime.FromTimeSpan(time));
            renderer.Render(frame);
            using Bitmap snapshot = renderer.Snapshot();
            return snapshot.Convert(BitmapColorType.Bgra8888, BitmapAlphaType.Premul, BitmapColorSpace.Srgb);
        });
    }

    public static (byte R, byte G, byte B, byte A) GetPixel(Bitmap bitmap, int x, int y)
    {
        Span<byte> row = bitmap.GetRow(y);
        return (row[x * 4 + 2], row[x * 4 + 1], row[x * 4], row[x * 4 + 3]);
    }

    /// <summary>Counts the pixels in the rectangle that satisfy <paramref name="predicate"/>.</summary>
    public static int Count(Bitmap frame, int x0, int y0, int x1, int y1, Func<(byte R, byte G, byte B, byte A), bool> predicate)
    {
        int count = 0;
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                if (predicate(GetPixel(frame, x, y)))
                    count++;
            }
        }

        return count;
    }

    public static bool IsDrawn((byte R, byte G, byte B, byte A) p) => p.A > 0;

    /// <summary>Saves the frame next to the test binaries for a human to look at.</summary>
    public static void SaveForInspection(Bitmap bitmap, string name)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "rendered");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, name);
        bitmap.Save(path, EncodedImageFormat.Png);
        TestContext.AddTestAttachment(path);
    }

    /// <summary>Whether two frames are the same, pixel for pixel.</summary>
    public static bool AreSame(Bitmap a, Bitmap b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
            return false;

        for (int y = 0; y < a.Height; y++)
        {
            if (!a.GetRow(y).SequenceEqual(b.GetRow(y)))
                return false;
        }

        return true;
    }
}
