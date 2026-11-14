using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

internal static class LineMath
{
    public const float Tau = MathF.PI * 2;

    public static float ToRadians(float degrees) => degrees * (MathF.PI / 180f);

    /// <summary>Replaces a value that is not a number with <paramref name="fallback"/> and clamps it.</summary>
    public static float Sanitize(float value, float min, float max, float fallback = 0)
    {
        if (float.IsNaN(value))
            return fallback;

        return Math.Clamp(value, min, max);
    }

    /// <summary>The distance from the centre of an axis-aligned ellipse to its edge in the direction <paramref name="angle"/>.</summary>
    public static float EllipseRadius(float radiusX, float radiusY, float angle)
    {
        if (radiusX <= 0 || radiusY <= 0)
            return 0;

        float x = MathF.Cos(angle) / radiusX;
        float y = MathF.Sin(angle) / radiusY;
        return 1 / MathF.Sqrt(x * x + y * y);
    }

    /// <summary>
    /// How far a ray from <paramref name="origin"/> in the direction (<paramref name="dx"/>, <paramref name="dy"/>)
    /// goes before it leaves the rectangle from the origin to <paramref name="frame"/>, or null when it never passes
    /// through it.
    /// </summary>
    public static float? ExitDistance(Size frame, Point origin, float dx, float dy)
    {
        float near = 0;
        float far = float.PositiveInfinity;
        if (!Slab(origin.X, dx, frame.Width, ref near, ref far)
            || !Slab(origin.Y, dy, frame.Height, ref near, ref far))
        {
            return null;
        }

        return far > 0 && float.IsFinite(far) ? far : null;
    }

    private static bool Slab(float origin, float direction, float size, ref float near, ref float far)
    {
        if (MathF.Abs(direction) < 1e-9f)
            return origin >= 0 && origin <= size;

        float t1 = -origin / direction;
        float t2 = (size - origin) / direction;
        if (t1 > t2)
            (t1, t2) = (t2, t1);

        near = MathF.Max(near, t1);
        far = MathF.Min(far, t2);
        return near <= far;
    }

    /// <summary>The distance from <paramref name="point"/> to the farthest corner of the frame.</summary>
    public static float FarthestCorner(Size frame, Point point)
    {
        float dx = MathF.Max(point.X, frame.Width - point.X);
        float dy = MathF.Max(point.Y, frame.Height - point.Y);
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Which drawing of the lines shows <paramref name="localTime"/> after the element starts, when they are redrawn
    /// every <paramref name="interval"/> seconds; always the first when the interval is not positive.
    /// </summary>
    public static long Generation(MediaTime localTime, float interval)
    {
        // An interval of more than a week is never reached; it also keeps the conversion below in range.
        if (!(interval > 0) || interval > 604800)
            return 0;

        // Rounded to a microsecond, 0.1 is 0.1 and not the float closest to it, so the drawings change exactly on
        // the frames the interval falls on.
        long step = (long)Math.Round(Math.Round((double)interval, 6) * MediaTime.FlicksPerSecond);
        if (step <= 0)
            return 0;

        // Floor, not truncation, so that a time before the start counts back from the first drawing.
        long quotient = Math.DivRem(localTime.Flicks, step, out long remainder);
        return remainder < 0 ? quotient - 1 : quotient;
    }

    /// <summary>Adds an axis-aligned ellipse as a polygon, with corners close enough to look round.</summary>
    public static void AddEllipse(FigureSet.Builder builder, Point center, float radiusX, float radiusY)
    {
        if (radiusX <= 0 || radiusY <= 0)
            return;

        // One corner per 2 px of the circumference keeps the chord within a few hundredths of a pixel of the arc.
        float circumference = MathF.PI * (radiusX + radiusY);
        int segments = Math.Clamp((int)MathF.Ceiling(circumference / 2), 32, 2048);
        for (int i = 0; i < segments; i++)
        {
            float angle = Tau * i / segments;
            builder.Add(center.X + radiusX * MathF.Cos(angle), center.Y + radiusY * MathF.Sin(angle));
        }

        builder.Close();
    }
}
