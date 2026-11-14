using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The evaluated values an urchin flash is built from; equal values build the same lines.</summary>
/// <param name="Count">How many lines go round the centre.</param>
/// <param name="ClearWidth">The width of the clear ellipse in the middle, in pixels.</param>
/// <param name="ClearHeight">The height of the clear ellipse, in pixels.</param>
/// <param name="Length">The longest a line is, in pixels.</param>
/// <param name="LengthVariation">How much shorter than that a line can be, in percent.</param>
/// <param name="Thickness">The widest a line is, in pixels.</param>
/// <param name="ThicknessVariation">How much thinner than that a line can be, in percent.</param>
/// <param name="SpacingVariation">How far a line can move from even spacing, in percent of the spacing.</param>
/// <param name="Jaggedness">How far past the ellipse a line can start, in percent of its radius.</param>
/// <param name="Shape">Which ends of a line come to a point.</param>
/// <param name="Rotation">The angle the lines are turned by, in degrees.</param>
/// <param name="Seed">The seed of the random spacing, lengths, widths and starts.</param>
/// <param name="Generation">Which drawing of the lines this is.</param>
internal readonly record struct UrchinFlashParameters(
    int Count,
    float ClearWidth,
    float ClearHeight,
    float Length,
    float LengthVariation,
    float Thickness,
    float ThicknessVariation,
    float SpacingVariation,
    float Jaggedness,
    FlashLineShape Shape,
    float Rotation,
    int Seed,
    long Generation);

internal static class UrchinFlashFigures
{
    /// <summary>The widest the lines can reach from the centre, horizontally and vertically.</summary>
    /// <remarks>It depends only on the sizes, so the flash keeps its place and bounds while the lines are redrawn.</remarks>
    public static Size GetExtent(in UrchinFlashParameters p)
    {
        float reach = p.Length + p.Thickness / 2;
        float jag = 1 + p.Jaggedness / 100;
        return new Size(p.ClearWidth / 2 * jag + reach, p.ClearHeight / 2 * jag + reach);
    }

    public static FigureSet Build(in UrchinFlashParameters p, Point center)
    {
        if (p.Count <= 0 || p.Length <= 0 || p.Thickness <= 0)
            return FigureSet.Empty;

        float radiusX = p.ClearWidth / 2;
        float radiusY = p.ClearHeight / 2;
        float rotation = LineMath.ToRadians(p.Rotation);
        float step = LineMath.Tau / p.Count;
        bool pointedInside = p.Shape is FlashLineShape.Both or FlashLineShape.Inner;
        bool pointedOutside = p.Shape is FlashLineShape.Both or FlashLineShape.Outer;
        var random = new LineRandom(p.Seed, p.Generation);
        var builder = new FigureSet.Builder();

        for (int i = 0; i < p.Count; i++)
        {
            float angle = rotation + step * (i + random.NextSigned(i, 0) * p.SpacingVariation / 100);
            float clear = LineMath.EllipseRadius(radiusX, radiusY, angle);
            float start = clear * (1 + p.Jaggedness / 100 * random.Next(i, 1));
            float width = p.Thickness * (1 - p.ThicknessVariation / 100 * random.Next(i, 2));
            // Shorter lines are more common than long ones, so a few long spines stand out of the outline.
            float shorten = random.Next(i, 3);
            float length = p.Length * (1 - p.LengthVariation / 100 * MathF.Sqrt(shorten));
            if (width < 0.05f || length < 0.5f)
                continue;

            float dx = MathF.Cos(angle);
            float dy = MathF.Sin(angle);
            var from = new Point(center.X + dx * start, center.Y + dy * start);
            var to = new Point(center.X + dx * (start + length), center.Y + dy * (start + length));
            LineShapes.AddStroke(builder, from, to, width / 2, pointedInside, pointedOutside, peak: 0.3f);
        }

        return builder.ToFigureSet();
    }
}
