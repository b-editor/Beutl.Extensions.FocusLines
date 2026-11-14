using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Graphics;
using Beutl.Media;
using Beutl.ProjectSystem;

namespace Beutl.Extensions.FocusLines.Tests;

// Renders the drawables through Beutl's own scene pipeline on a 1920x1080 scene with nothing below them.
public class RenderTests
{
    private static Bitmap Render(Drawable drawable, double seconds = 0)
    {
        Scene scene = HeadlessScene.Create(drawable);
        return HeadlessScene.Render(scene, TimeSpan.FromSeconds(seconds));
    }

    private static int Drawn(Bitmap frame, int x0, int y0, int x1, int y1)
        => HeadlessScene.Count(frame, x0, y0, x1, y1, HeadlessScene.IsDrawn);

    private static bool IsWhite((byte R, byte G, byte B, byte A) p) => p.A > 250 && p.R > 250 && p.G > 250 && p.B > 250;

    private static bool IsBlack((byte R, byte G, byte B, byte A) p) => p.A > 250 && p.R < 5 && p.G < 5 && p.B < 5;

    [Test]
    public void FocusLinesLeaveTheMiddleClearAndReachTheEdges()
    {
        using Bitmap frame = Render(new FocusLinesDrawable());

        Assert.That(Drawn(frame, 760, 440, 1160, 640), Is.Zero);
        Assert.That(HeadlessScene.Count(frame, 0, 0, 1920, 40, IsWhite), Is.GreaterThan(5000));
        Assert.That(HeadlessScene.Count(frame, 0, 1040, 1920, 1080, IsWhite), Is.GreaterThan(5000));
        Assert.That(HeadlessScene.Count(frame, 0, 0, 40, 1080, IsWhite), Is.GreaterThan(2000));
        Assert.That(HeadlessScene.Count(frame, 1880, 0, 1920, 1080, IsWhite), Is.GreaterThan(2000));
    }

    [Test]
    public void FocusLinesMoveWithTheirCenter()
    {
        var drawable = new FocusLinesDrawable();
        drawable.Center.Value = new Point(-600, 0);

        using Bitmap frame = Render(drawable);

        Assert.That(Drawn(frame, 260, 440, 460, 640), Is.Zero);
        Assert.That(Drawn(frame, 1300, 440, 1500, 640), Is.GreaterThan(1000));
    }

    [Test]
    public void FocusLinesAreRedrawnOnlyAtTheirInterval()
    {
        var drawable = new FocusLinesDrawable();
        drawable.UpdateInterval.Value = 0.5f;
        Scene scene = HeadlessScene.Create(drawable);

        using Bitmap first = HeadlessScene.Render(scene, TimeSpan.FromSeconds(0.1));
        using Bitmap same = HeadlessScene.Render(scene, TimeSpan.FromSeconds(0.4));
        using Bitmap next = HeadlessScene.Render(scene, TimeSpan.FromSeconds(0.6));

        Assert.That(HeadlessScene.AreSame(first, same), Is.True);
        Assert.That(HeadlessScene.AreSame(first, next), Is.False);
    }

    [Test]
    public void UrchinFlashIsCenteredLikeAShape()
    {
        var drawable = new UrchinFlashDrawable();
        drawable.UpdateInterval.Value = 0;

        using Bitmap frame = Render(drawable);
        HeadlessScene.SaveForInspection(frame, "urchin-alone.png");

        // White lines outlined in black round a clear middle, within the reach of the longest line.
        Assert.That(Drawn(frame, 860, 440, 1060, 640), Is.Zero);
        Assert.That(HeadlessScene.Count(frame, 560, 140, 1360, 940, IsWhite), Is.GreaterThan(5000));
        Assert.That(HeadlessScene.Count(frame, 560, 140, 1360, 940, IsBlack), Is.GreaterThan(1000));
        Assert.That(Drawn(frame, 0, 0, 500, 1080), Is.Zero);
        Assert.That(Drawn(frame, 1420, 0, 1920, 1080), Is.Zero);
    }

    [Test]
    public void BetaFlashFillsAroundRaysOfLight()
    {
        using Bitmap frame = Render(new BetaFlashDrawable());

        Assert.That(IsBlack(HeadlessScene.GetPixel(frame, 5, 5)), Is.True);
        Assert.That(IsBlack(HeadlessScene.GetPixel(frame, 1914, 1074)), Is.True);
        Assert.That(Drawn(frame, 860, 490, 1060, 590), Is.Zero);
        Assert.That(HeadlessScene.Count(frame, 0, 0, 1920, 1080, IsWhite), Is.GreaterThan(20000));
    }

    [Test]
    public void BetaFlashWithoutLightShowsWhatIsBelowThroughTheRays()
    {
        var drawable = new BetaFlashDrawable();
        drawable.Flash.Value = null;

        using Bitmap frame = Render(drawable);

        Assert.That(HeadlessScene.Count(frame, 0, 0, 1920, 1080, IsWhite), Is.Zero);
        Assert.That(HeadlessScene.Count(frame, 0, 0, 1920, 1080, IsBlack), Is.GreaterThan(500000));
        Assert.That(Drawn(frame, 860, 490, 1060, 590), Is.Zero);
    }

    [Test]
    public void SpeedLinesStreamUnlessTheyAreStopped()
    {
        var moving = new SpeedLinesDrawable();
        Scene scene = HeadlessScene.Create(moving);
        using Bitmap a = HeadlessScene.Render(scene, TimeSpan.FromSeconds(1));
        using Bitmap b = HeadlessScene.Render(scene, TimeSpan.FromSeconds(1.5));
        Assert.That(HeadlessScene.AreSame(a, b), Is.False);

        var stopped = new SpeedLinesDrawable();
        stopped.Speed.Value = 0;
        Scene still = HeadlessScene.Create(stopped);
        using Bitmap c = HeadlessScene.Render(still, TimeSpan.FromSeconds(1));
        using Bitmap d = HeadlessScene.Render(still, TimeSpan.FromSeconds(1.5));
        Assert.That(HeadlessScene.AreSame(c, d), Is.True);
        Assert.That(Drawn(c, 0, 0, 1920, 1080), Is.GreaterThan(10000));
    }

    [Test]
    public void SunburstTurnsAtItsSpeed()
    {
        var drawable = new SunburstDrawable();
        drawable.Fill.Value = new SolidColorBrush(Colors.White);
        drawable.RotationSpeed.Value = 360f / 16;
        Scene scene = HeadlessScene.Create(drawable);

        // Sixteen rays turning a sixteenth of a turn a second look the same each second, and not half way.
        using Bitmap start = HeadlessScene.Render(scene, TimeSpan.Zero);
        using Bitmap half = HeadlessScene.Render(scene, TimeSpan.FromSeconds(0.5));
        using Bitmap whole = HeadlessScene.Render(scene, TimeSpan.FromSeconds(1));

        int differentHalf = Different(start, half);
        int differentWhole = Different(start, whole);
        Assert.That(differentHalf, Is.GreaterThan(500000));
        Assert.That(differentWhole, Is.LessThan(differentHalf / 100));

        static int Different(Bitmap a, Bitmap b)
        {
            int count = 0;
            for (int y = 0; y < a.Height; y++)
            {
                for (int x = 0; x < a.Width; x++)
                {
                    if (Math.Abs(HeadlessScene.GetPixel(a, x, y).A - HeadlessScene.GetPixel(b, x, y).A) > 64)
                        count++;
                }
            }

            return count;
        }
    }
}
