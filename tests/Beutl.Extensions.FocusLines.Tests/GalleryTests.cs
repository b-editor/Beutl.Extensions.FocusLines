using Beutl.Engine;
using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Graphics;
using Beutl.Media;
using Beutl.ProjectSystem;

namespace Beutl.Extensions.FocusLines.Tests;

// Renders each kind of line at its defaults and in a few variations, for a human to look at. The images are saved
// in bin/Debug/net11.0/rendered/.
[Category("Gallery")]
public class GalleryTests
{
    private static readonly Color s_dark = Color.FromArgb(255, 40, 44, 60);
    private static readonly Color s_light = Color.FromArgb(255, 236, 232, 222);

    private static void Save(string name, Color background, EngineObject drawable, double seconds = 0)
    {
        Scene scene = HeadlessScene.Create(HeadlessScene.Background(background), drawable);
        using Bitmap frame = HeadlessScene.Render(scene, TimeSpan.FromSeconds(seconds));
        HeadlessScene.SaveForInspection(frame, name);
    }

    [Test]
    public void FocusLines()
    {
        Save("focus-default.png", s_dark, new FocusLinesDrawable());

        var black = new FocusLinesDrawable();
        black.Fill.Value = new SolidColorBrush(Colors.Black);
        Save("focus-black.png", s_light, black);

        var moved = new FocusLinesDrawable();
        moved.Center.Value = new Point(-420, -160);
        moved.ClearWidth.Value = 30;
        moved.ClearHeight.Value = 40;
        Save("focus-moved.png", s_dark, moved);

        var twisted = new FocusLinesDrawable();
        twisted.Twist.Value = 90;
        twisted.Count.Value = 90;
        Save("focus-twist.png", s_dark, twisted);

        var outlined = new FocusLinesDrawable();
        outlined.Thickness.Value = 28;
        outlined.Count.Value = 70;
        var pen = new Pen();
        pen.Brush.Value = new SolidColorBrush(Colors.Black);
        pen.Thickness.Value = 3;
        pen.StrokeJoin.Value = StrokeJoin.Round;
        outlined.Pen.Value = pen;
        Save("focus-outlined.png", s_light, outlined);
    }

    [Test]
    public void UrchinFlash()
    {
        Save("urchin-default.png", s_dark, new UrchinFlashDrawable());
        Save("urchin-default-light.png", s_light, new UrchinFlashDrawable());

        var black = new UrchinFlashDrawable();
        black.Fill.Value = new SolidColorBrush(Colors.Black);
        black.Pen.Value = null;
        Save("urchin-black.png", s_light, black);

        var spikes = new UrchinFlashDrawable();
        spikes.Shape.Value = FlashLineShape.Outer;
        spikes.Count.Value = 120;
        spikes.Thickness.Value = 14;
        Save("urchin-outer.png", s_light, spikes);

        var inner = new UrchinFlashDrawable();
        inner.Shape.Value = FlashLineShape.Inner;
        inner.Fill.Value = new SolidColorBrush(Colors.Black);
        inner.Pen.Value = null;
        Save("urchin-inner.png", s_light, inner);
    }

    [Test]
    public void BetaFlash()
    {
        Save("beta-default.png", s_light, new BetaFlashDrawable());

        var cut = new BetaFlashDrawable();
        cut.Flash.Value = null;
        cut.Center.Value = new Point(300, 80);
        Save("beta-cut.png", s_light, cut);
    }

    [Test]
    public void SpeedLines()
    {
        Save("speed-default.png", s_dark, new SpeedLinesDrawable());

        var diagonal = new SpeedLinesDrawable();
        diagonal.Angle.Value = -30;
        diagonal.Shape.Value = SpeedLineShape.Back;
        diagonal.ClearBand.Value = 30;
        diagonal.Fill.Value = new SolidColorBrush(Colors.Black);
        Save("speed-diagonal.png", s_light, diagonal, 1.3);
    }

    [Test]
    public void Sunburst()
    {
        Save("sunburst-default.png", s_dark, new SunburstDrawable());

        var colored = new SunburstDrawable();
        colored.Fill.Value = new SolidColorBrush(Color.FromArgb(255, 255, 214, 64));
        colored.Background.Value = new SolidColorBrush(Color.FromArgb(255, 255, 170, 30));
        colored.Twist.Value = 60;
        colored.Count.Value = 20;
        colored.ClearWidth.Value = 20;
        colored.ClearHeight.Value = 30;
        Save("sunburst-colored.png", s_dark, colored);
    }
}
