using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The evaluated values a beta flash is built from; equal values build the same figures.</summary>
/// <param name="Frame">The size of the screen the flash covers.</param>
/// <param name="Center">The centre of the flash, from the centre of the screen.</param>
/// <param name="Count">How many rays go round the centre.</param>
/// <param name="ClearWidth">The width of the clear ellipse round the centre, in percent of the screen width.</param>
/// <param name="ClearHeight">The height of the clear ellipse, in percent of the screen height.</param>
/// <param name="Length">The longest a ray reaches, in percent of the way from the ellipse to the edge of the screen.</param>
/// <param name="LengthVariation">How much shorter than that a ray can be, in percent.</param>
/// <param name="Thickness">The widest a ray is, at its foot on the ellipse, in pixels.</param>
/// <param name="ThicknessVariation">How much thinner than that a ray can be, in percent.</param>
/// <param name="SpacingVariation">How far a ray can move from even spacing, in percent of the spacing.</param>
/// <param name="Rotation">The angle the rays are turned by, in degrees.</param>
/// <param name="Seed">The seed of the random spacing, lengths and widths.</param>
/// <param name="Generation">Which drawing of the rays this is.</param>
internal readonly record struct BetaFlashParameters(
    Size Frame,
    Point Center,
    int Count,
    float ClearWidth,
    float ClearHeight,
    float Length,
    float LengthVariation,
    float Thickness,
    float ThicknessVariation,
    float SpacingVariation,
    float Rotation,
    int Seed,
    long Generation);

/// <summary>The figures of a beta flash.</summary>
/// <param name="Clear">The clear ellipse round the centre.</param>
/// <param name="Rays">The rays, which start inside the ellipse so that their feet are hidden in it.</param>
/// <param name="Flash">The ellipse and the rays together: everything the fill leaves out.</param>
internal sealed record BetaFlashFigureSets(FigureSet Clear, FigureSet Rays, FigureSet Flash);

internal static class BetaFlashFigures
{
    public static BetaFlashFigureSets Build(in BetaFlashParameters p)
    {
        Size frame = p.Frame;
        var center = new Point(frame.Width / 2 + p.Center.X, frame.Height / 2 + p.Center.Y);
        float radiusX = frame.Width * p.ClearWidth / 200;
        float radiusY = frame.Height * p.ClearHeight / 200;

        var clear = new FigureSet.Builder();
        var rays = new FigureSet.Builder();
        var flash = new FigureSet.Builder();
        LineMath.AddEllipse(clear, center, radiusX, radiusY);
        LineMath.AddEllipse(flash, center, radiusX, radiusY);

        if (p.Count > 0 && p.Thickness > 0 && p.Length > 0 && frame.Width > 0 && frame.Height > 0)
        {
            float rotation = LineMath.ToRadians(p.Rotation);
            float step = LineMath.Tau / p.Count;
            var random = new LineRandom(p.Seed, p.Generation);
            for (int i = 0; i < p.Count; i++)
            {
                float angle = rotation + step * (i + random.NextSigned(i, 0) * p.SpacingVariation / 100);
                float dx = MathF.Cos(angle);
                float dy = MathF.Sin(angle);
                float edge = LineMath.EllipseRadius(radiusX, radiusY, angle);
                if (LineMath.ExitDistance(frame, center, dx, dy) is not { } exit || exit <= edge)
                    continue;

                float shorten = random.Next(i, 3);
                float length = (exit - edge) * p.Length / 100 * (1 - p.LengthVariation / 100 * MathF.Sqrt(shorten));
                float width = p.Thickness * (1 - p.ThicknessVariation / 100 * random.Next(i, 2));
                if (width < 0.05f || length < 0.5f)
                    continue;

                // The foot starts a little inside the ellipse, which hides it, so it meets the edge without a gap.
                float foot = edge * 0.9f;
                var from = new Point(center.X + dx * foot, center.Y + dy * foot);
                var to = new Point(center.X + dx * (edge + length), center.Y + dy * (edge + length));
                LineShapes.AddStroke(rays, from, to, width / 2, pointedFrom: false, pointedTo: true);
                LineShapes.AddStroke(flash, from, to, width / 2, pointedFrom: false, pointedTo: true);
            }
        }

        return new BetaFlashFigureSets(clear.ToFigureSet(), rays.ToFigureSet(), flash.ToFigureSet());
    }
}
