using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The evaluated values a sunburst is built from; equal values build the same rays.</summary>
/// <param name="Frame">The size of the screen the rays cover.</param>
/// <param name="Center">Where the rays meet, from the centre of the screen.</param>
/// <param name="Count">How many rays go round the centre.</param>
/// <param name="RayWidth">How much of each ray's share of the turn the ray fills, in percent.</param>
/// <param name="Rotation">The angle the rays are turned by, in degrees, with the turn they have made over time.</param>
/// <param name="Twist">How far a ray turns round the centre over half the screen's diagonal, in degrees.</param>
internal readonly record struct SunburstParameters(
    Size Frame,
    Point Center,
    int Count,
    float RayWidth,
    float Rotation,
    float Twist);

internal static class SunburstFigures
{
    public static FigureSet Build(in SunburstParameters p)
    {
        Size frame = p.Frame;
        if (p.Count <= 0 || p.RayWidth <= 0 || frame.Width <= 0 || frame.Height <= 0)
            return FigureSet.Empty;

        var center = new Point(frame.Width / 2 + p.Center.X, frame.Height / 2 + p.Center.Y);
        float halfDiagonal = MathF.Sqrt(frame.Width * frame.Width + frame.Height * frame.Height) / 2;
        // Past the farthest corner, so the outer arc is never on the screen.
        float far = LineMath.FarthestCorner(frame, center) * 1.1f + 2;
        float twist = LineMath.ToRadians(p.Twist) / halfDiagonal;
        float rotation = LineMath.ToRadians(p.Rotation);
        float step = LineMath.Tau / p.Count;
        float ray = step * Math.Clamp(p.RayWidth / 100, 0, 1);
        // The outer arc is off the screen, so it can be coarse; a twisted edge is on it and needs a corner per half
        // degree of turn, and per 16 px.
        int arcSegments = Math.Max(1, (int)MathF.Ceiling(ray / LineMath.ToRadians(3)));
        int edgeSegments = twist == 0
            ? 1
            : Math.Clamp((int)MathF.Ceiling(MathF.Max(MathF.Abs(twist * far) / LineMath.ToRadians(0.5f), far / 16)), 2, 2000);
        var builder = new FigureSet.Builder();

        Point At(float radius, float angle)
        {
            float a = angle + twist * radius;
            return new Point(center.X + radius * MathF.Cos(a), center.Y + radius * MathF.Sin(a));
        }

        for (int k = 0; k < p.Count; k++)
        {
            float start = rotation + step * k;
            float end = start + ray;
            builder.Add(center);
            for (int i = 1; i <= edgeSegments; i++)
                builder.Add(At(far * i / edgeSegments, start));

            for (int i = 1; i <= arcSegments; i++)
                builder.Add(At(far, start + ray * i / arcSegments));

            for (int i = edgeSegments - 1; i >= 1; i--)
                builder.Add(At(far * i / edgeSegments, end));

            builder.Close();
        }

        return builder.ToFigureSet();
    }
}
