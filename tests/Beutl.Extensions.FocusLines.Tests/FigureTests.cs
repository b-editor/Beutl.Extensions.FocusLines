using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Tests;

// The figures are built from the evaluated values alone, so they are tested without rendering.
public class FigureTests
{
    private static readonly Size s_frame = new(1920, 1080);

    private static FocusLinesParameters Focus(Point center = default, float twist = 0, int seed = 0, long generation = 0)
        => new(s_frame, center, 120, 16, 70, 100, 50, 50, 40, 0, twist, seed, generation);

    private static IEnumerable<Point> Points(FigureSet set)
    {
        for (int i = 0; i < set.Count; i++)
        {
            foreach (Point point in set[i].ToArray())
                yield return point;
        }
    }

    private static Point Centroid(ReadOnlySpan<Point> figure)
    {
        float x = 0, y = 0;
        foreach (Point point in figure)
        {
            x += point.X;
            y += point.Y;
        }

        return new Point(x / figure.Length, y / figure.Length);
    }

    private static bool SameFigures(FigureSet a, FigureSet b) => Points(a).SequenceEqual(Points(b));

    [Test]
    public void FocusLinesStayOutsideTheClearEllipse()
    {
        FigureSet set = FocusLinesFigures.Build(Focus());

        Assert.That(set.Count, Is.InRange(100, 120));
        foreach (Point p in Points(set))
        {
            float x = (p.X - 960) / 480;
            float y = (p.Y - 540) / 270;
            Assert.That(x * x + y * y, Is.GreaterThanOrEqualTo(0.999f), $"{p} is inside the clear area");
        }
    }

    [Test]
    public void FocusLinesReachPastTheEdgesOfTheScreen()
    {
        FigureSet set = FocusLinesFigures.Build(Focus());

        // Every line ends beyond the farthest corner, so its far end is never seen.
        float corner = MathF.Sqrt(960 * 960 + 540 * 540);
        for (int i = 0; i < set.Count; i++)
        {
            float farthest = set[i].ToArray().Max(p => MathF.Sqrt((p.X - 960) * (p.X - 960) + (p.Y - 540) * (p.Y - 540)));
            Assert.That(farthest, Is.GreaterThan(corner));
        }
    }

    [Test]
    public void FocusLinesFollowTheCenter()
    {
        FigureSet set = FocusLinesFigures.Build(Focus(center: new Point(-500, 200)));

        foreach (Point p in Points(set))
        {
            float x = (p.X - 460) / 480;
            float y = (p.Y - 740) / 270;
            Assert.That(x * x + y * y, Is.GreaterThanOrEqualTo(0.999f));
        }
    }

    [Test]
    public void TheSameValuesDrawTheSameLines()
    {
        Assert.That(SameFigures(FocusLinesFigures.Build(Focus()), FocusLinesFigures.Build(Focus())), Is.True);
        Assert.That(SameFigures(FocusLinesFigures.Build(Focus()), FocusLinesFigures.Build(Focus(seed: 1))), Is.False);
        Assert.That(SameFigures(FocusLinesFigures.Build(Focus()), FocusLinesFigures.Build(Focus(generation: 1))), Is.False);
    }

    [Test]
    public void TwistedLinesCurveRoundTheCenter()
    {
        FigureSet straight = FocusLinesFigures.Build(Focus());
        FigureSet twisted = FocusLinesFigures.Build(Focus(twist: 90));

        Assert.That(twisted.Count, Is.EqualTo(straight.Count));
        // A straight line is a triangle; a curved one follows its spiral with many corners.
        Assert.That(straight[0].Length, Is.EqualTo(3));
        Assert.That(twisted[0].Length, Is.GreaterThan(40));

        // The first corner is the start; the middle corner is on the far end, which has turned clockwise.
        Point start = twisted[0][0];
        Point end = twisted[0][twisted[0].Length / 2];
        float startAngle = MathF.Atan2(start.Y - 540, start.X - 960);
        float endAngle = MathF.Atan2(end.Y - 540, end.X - 960);
        float turned = (endAngle - startAngle + 3 * MathF.PI) % (2 * MathF.PI) - MathF.PI;
        Assert.That(turned, Is.GreaterThan(0.5f));
    }

    [Test]
    public void FocusLinesFromACenterOffTheScreenOnlyKeepTheLinesThatCrossIt()
    {
        FigureSet set = FocusLinesFigures.Build(Focus(center: new Point(-1400, 0)));

        // The centre is 440 px left of the screen, so only the lines heading right cross it.
        Assert.That(set.Count, Is.InRange(1, 60));
    }

    [TestCase(0.0, 0)]
    [TestCase(0.0999, 0)]
    [TestCase(0.1, 1)]
    [TestCase(0.35, 3)]
    [TestCase(-0.05, -1)]
    public void TheLinesAreRedrawnEveryInterval(double seconds, long expected)
    {
        Assert.That(LineMath.Generation(MediaTime.FromSeconds(seconds), 0.1f), Is.EqualTo(expected));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    public void WithoutAnIntervalTheLinesStay(float interval)
    {
        Assert.That(LineMath.Generation(MediaTime.FromSeconds(12.3), interval), Is.Zero);
    }

    [Test]
    public void AnUrchinFlashStaysInItsBounds()
    {
        var p = new UrchinFlashParameters(260, 360, 240, 260, 60, 7, 50, 100, 10, FlashLineShape.Both, 0, 0, 0);
        Size extent = UrchinFlashFigures.GetExtent(p);
        var center = new Point(extent.Width, extent.Height);
        FigureSet set = UrchinFlashFigures.Build(p, center);

        Assert.That(set.Count, Is.EqualTo(260));
        foreach (Point point in Points(set))
        {
            Assert.That(point.X, Is.InRange(-0.01f, extent.Width * 2 + 0.01f));
            Assert.That(point.Y, Is.InRange(-0.01f, extent.Height * 2 + 0.01f));
            float x = (point.X - center.X) / 180;
            float y = (point.Y - center.Y) / 120;
            Assert.That(x * x + y * y, Is.GreaterThanOrEqualTo(0.99f), $"{point} is inside the clear area");
        }
    }

    [Test]
    public void BetaFlashRaysStartInsideTheClearArea()
    {
        var p = new BetaFlashParameters(s_frame, default, 90, 36, 36, 75, 70, 30, 50, 100, 0, 0, 0);
        BetaFlashFigureSets sets = BetaFlashFigures.Build(p);

        Assert.That(sets.Clear.Count, Is.EqualTo(1));
        Assert.That(sets.Rays.Count, Is.EqualTo(90));
        Assert.That(sets.Flash.Count, Is.EqualTo(91));
        // The feet of the rays are hidden in the ellipse, so no gap shows between them and the clear area.
        for (int i = 0; i < sets.Rays.Count; i++)
        {
            float nearest = sets.Rays[i].ToArray().Min(q =>
            {
                float x = (q.X - 960) / (1920 * 0.18f);
                float y = (q.Y - 540) / (1080 * 0.18f);
                return x * x + y * y;
            });
            Assert.That(nearest, Is.LessThan(1));
        }
    }

    [Test]
    public void SpeedLinesStreamInTheirDirection()
    {
        SpeedLinesParameters At(double time) => new(
            s_frame, 0, 90, 45, 60, 8, 70, SpeedLineShape.Both, 1000, 0, 0, 0, 0, time);

        FigureSet before = SpeedLinesFigures.Build(At(1));
        FigureSet after = SpeedLinesFigures.Build(At(1.01));

        // Every line that has not just wrapped round has moved 10 px to the right, and not up or down.
        Assert.That(after.Count, Is.EqualTo(before.Count));
        int moved = 0;
        for (int i = 0; i < before.Count; i++)
        {
            Point a = Centroid(before[i]);
            Point b = Centroid(after[i]);
            if (MathF.Abs(b.X - a.X - 10) < 0.05f && MathF.Abs(b.Y - a.Y) < 0.05f)
                moved++;
        }

        Assert.That(moved, Is.GreaterThanOrEqualTo(before.Count - 2));
    }

    [Test]
    public void SpeedLinesLeaveTheClearBandEmpty()
    {
        var p = new SpeedLinesParameters(s_frame, 0, 200, 45, 60, 8, 70, SpeedLineShape.Both, 1000, 40, 30, 0, 0, 2.5);
        FigureSet set = SpeedLinesFigures.Build(p);

        Assert.That(set.Count, Is.EqualTo(200));
        // The band is 30 % of the screen's height round its middle; a line reaches 4 px either side of its axis.
        foreach (Point point in Points(set))
            Assert.That(MathF.Abs(point.Y - 540), Is.GreaterThanOrEqualTo(162 - 4.01f));
    }

    [Test]
    public void SunburstHasOneRayPerShare()
    {
        var p = new SunburstParameters(s_frame, default, 16, 50, 0, 0);
        Assert.That(SunburstFigures.Build(p).Count, Is.EqualTo(16));
        Assert.That(SunburstFigures.Build(p with { RayWidth = 0 }).Count, Is.Zero);
        Assert.That(SunburstFigures.Build(p with { Twist = 45 }).Count, Is.EqualTo(16));
    }

    [Test]
    public void BuilderTurnsEveryFigureTheSameWayAndDropsEmptyOnes()
    {
        var builder = new FigureSet.Builder();
        // Clockwise and anticlockwise triangles.
        builder.Add(0, 0);
        builder.Add(10, 0);
        builder.Add(0, 10);
        builder.Close();
        builder.Add(0, 0);
        builder.Add(0, 10);
        builder.Add(10, 0);
        builder.Close();
        // A line with no area, and a figure that is not a number.
        builder.Add(0, 0);
        builder.Add(5, 5);
        builder.Add(10, 10);
        builder.Close();
        builder.Add(float.NaN, 0);
        builder.Add(10, 0);
        builder.Add(0, 10);
        builder.Close();
        FigureSet set = builder.ToFigureSet();

        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(SignedArea(set[0]), Is.EqualTo(50).Within(1e-3));
        Assert.That(SignedArea(set[1]), Is.EqualTo(50).Within(1e-3));

        static double SignedArea(ReadOnlySpan<Point> figure)
        {
            double twice = 0;
            for (int i = 0; i < figure.Length; i++)
            {
                Point a = figure[i];
                Point b = figure[(i + 1) % figure.Length];
                twice += (double)a.X * b.Y - (double)b.X * a.Y;
            }

            return twice / 2;
        }
    }
}
