using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The evaluated values the focus lines are built from; equal values build the same lines.</summary>
/// <param name="Frame">The size of the screen the lines cover.</param>
/// <param name="Center">Where the lines converge, from the centre of the screen.</param>
/// <param name="Count">How many lines go round the centre.</param>
/// <param name="Thickness">The widest a line is where it leaves the screen.</param>
/// <param name="ThicknessVariation">How much thinner than that a line can be, in percent.</param>
/// <param name="SpacingVariation">How far a line can move from even spacing, in percent of the spacing.</param>
/// <param name="ClearWidth">The width of the clear ellipse around the centre, in percent of the screen width.</param>
/// <param name="ClearHeight">The height of the clear ellipse, in percent of the screen height.</param>
/// <param name="Jaggedness">How far past the ellipse a line can start, in percent of its radius.</param>
/// <param name="Rotation">The angle the lines are turned by, in degrees.</param>
/// <param name="Twist">How far a line turns round the centre over half the screen's diagonal, in degrees.</param>
/// <param name="Seed">The seed of the random spacing, widths and starts.</param>
/// <param name="Generation">Which drawing of the lines this is.</param>
internal readonly record struct FocusLinesParameters(
    Size Frame,
    Point Center,
    int Count,
    float Thickness,
    float ThicknessVariation,
    float SpacingVariation,
    float ClearWidth,
    float ClearHeight,
    float Jaggedness,
    float Rotation,
    float Twist,
    int Seed,
    long Generation);

internal static class FocusLinesFigures
{
    public static FigureSet Build(in FocusLinesParameters p)
    {
        Size frame = p.Frame;
        if (p.Count <= 0 || frame.Width <= 0 || frame.Height <= 0 || p.Thickness <= 0)
            return FigureSet.Empty;

        var center = new Point(frame.Width / 2 + p.Center.X, frame.Height / 2 + p.Center.Y);
        float radiusX = frame.Width * p.ClearWidth / 200;
        float radiusY = frame.Height * p.ClearHeight / 200;
        // Every line runs on past the farthest corner, so its end is never on the screen.
        float far = LineMath.FarthestCorner(frame, center) + p.Thickness;
        float halfDiagonal = MathF.Sqrt(frame.Width * frame.Width + frame.Height * frame.Height) / 2;
        float twist = LineMath.ToRadians(p.Twist) / halfDiagonal;
        float rotation = LineMath.ToRadians(p.Rotation);
        float step = LineMath.Tau / p.Count;
        var random = new LineRandom(p.Seed, p.Generation);
        var builder = new FigureSet.Builder();

        for (int i = 0; i < p.Count; i++)
        {
            float angle = rotation + step * (i + random.NextSigned(i, 0) * p.SpacingVariation / 100);
            float clear = LineMath.EllipseRadius(radiusX, radiusY, angle);
            // Most lines start near the edge of the clear area and a few well past it, as when drawn by hand.
            float jag = random.Next(i, 1);
            float start = clear * (1 + p.Jaggedness / 100 * jag * jag);
            float width = p.Thickness * (1 - p.ThicknessVariation / 100 * random.Next(i, 2));
            if (width < 0.05f)
                continue;

            var line = new Line(center, angle, clear, start, far, width);
            if (twist == 0)
                AddStraight(builder, frame, line);
            else
                AddCurved(builder, frame, line, twist);
        }

        return builder.ToFigureSet();
    }

    // A line from Start to End along Angle, pointed at the start and Width wide where it leaves the screen.
    private readonly record struct Line(Point Center, float Angle, float Clear, float Start, float End, float Width);

    private static void AddStraight(FigureSet.Builder builder, Size frame, in Line line)
    {
        float dx = MathF.Cos(line.Angle);
        float dy = MathF.Sin(line.Angle);
        if (LineMath.ExitDistance(frame, line.Center, dx, dy) is not { } exit || exit <= line.Start + 0.5f)
            return;

        float half = line.Width / 2 * (line.End - line.Start) / (exit - line.Start);
        Point c = line.Center;
        builder.Add(c.X + dx * line.Start, c.Y + dy * line.Start);
        builder.Add(c.X + dx * line.End - dy * half, c.Y + dy * line.End + dx * half);
        builder.Add(c.X + dx * line.End + dy * half, c.Y + dy * line.End - dx * half);
        builder.Close();
    }

    // The line turns round the centre as it goes out, by `twist` radians per pixel past the clear area.
    private static void AddCurved(FigureSet.Builder builder, Size frame, in Line line, float twist)
    {
        Line l = line;
        float At(float r) => l.Angle + twist * (r - l.Clear);
        Point On(float r) => new(l.Center.X + r * MathF.Cos(At(r)), l.Center.Y + r * MathF.Sin(At(r)));
        bool Inside(Point p) => p.X >= 0 && p.Y >= 0 && p.X <= frame.Width && p.Y <= frame.Height;

        // Sample finely enough that the polygon follows the curve: a corner per degree of turn, and per 32 px.
        float turn = MathF.Abs(twist * (line.End - line.Start));
        int samples = Math.Clamp(
            (int)MathF.Ceiling(MathF.Max(turn / LineMath.ToRadians(1), (line.End - line.Start) / 32)) + 2, 4, 360);
        float span = line.End - line.Start;

        // Where the line last leaves the screen; the line is Width wide there.
        float exit = float.NaN;
        for (int k = samples; k >= 0; k--)
        {
            float r = line.Start + span * k / samples;
            if (!Inside(On(r)))
                continue;

            float outside = line.Start + span * Math.Min(k + 1, samples) / samples;
            for (int n = 0; n < 12; n++)
            {
                float mid = (r + outside) / 2;
                if (Inside(On(mid)))
                    r = mid;
                else
                    outside = mid;
            }

            exit = r;
            break;
        }

        if (!(exit > line.Start + 0.5f))
            return;

        Span<Point> left = stackalloc Point[samples + 1];
        Span<Point> right = stackalloc Point[samples + 1];
        for (int k = 1; k <= samples; k++)
        {
            float r = line.Start + span * k / samples;
            float a = At(r);
            float cos = MathF.Cos(a);
            float sin = MathF.Sin(a);
            // The tangent of the spiral, then the normal to it.
            float tx = cos - r * twist * sin;
            float ty = sin + r * twist * cos;
            float length = MathF.Sqrt(tx * tx + ty * ty);
            float nx = -ty / length;
            float ny = tx / length;
            float half = line.Width / 2 * (r - line.Start) / (exit - line.Start);
            float px = line.Center.X + r * cos;
            float py = line.Center.Y + r * sin;
            left[k] = new Point(px + nx * half, py + ny * half);
            right[k] = new Point(px - nx * half, py - ny * half);
        }

        builder.Add(On(line.Start));
        for (int k = 1; k <= samples; k++)
            builder.Add(left[k]);

        for (int k = samples; k >= 1; k--)
            builder.Add(right[k]);

        builder.Close();
    }
}
