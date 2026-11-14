using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The evaluated values speed lines are built from; equal values build the same lines.</summary>
/// <param name="Frame">The size of the screen the lines cover.</param>
/// <param name="Angle">The direction the lines point and move in, in degrees clockwise from the right.</param>
/// <param name="Count">How many lines there are.</param>
/// <param name="Length">The longest a line is, in percent of the screen's length in the lines' direction.</param>
/// <param name="LengthVariation">How much shorter than that a line can be, in percent.</param>
/// <param name="Thickness">The widest a line is, in pixels.</param>
/// <param name="ThicknessVariation">How much thinner than that a line can be, in percent.</param>
/// <param name="Shape">Which ends of a line come to a point.</param>
/// <param name="Speed">How fast the fastest line moves, in pixels per second.</param>
/// <param name="SpeedVariation">How much slower than that a line can be, in percent.</param>
/// <param name="ClearBand">The width of the clear band along the middle, in percent of the screen across the lines.</param>
/// <param name="Seed">The seed of the random positions, lengths, widths and speeds.</param>
/// <param name="Generation">Which drawing of the lines this is.</param>
/// <param name="Time">The seconds since the element started, for the moving lines.</param>
internal readonly record struct SpeedLinesParameters(
    Size Frame,
    float Angle,
    int Count,
    float Length,
    float LengthVariation,
    float Thickness,
    float ThicknessVariation,
    SpeedLineShape Shape,
    float Speed,
    float SpeedVariation,
    float ClearBand,
    int Seed,
    long Generation,
    double Time);

internal static class SpeedLinesFigures
{
    public static FigureSet Build(in SpeedLinesParameters p)
    {
        Size frame = p.Frame;
        if (p.Count <= 0 || p.Thickness <= 0 || p.Length <= 0 || frame.Width <= 0 || frame.Height <= 0)
            return FigureSet.Empty;

        float angle = LineMath.ToRadians(p.Angle);
        float ux = MathF.Cos(angle);
        float uy = MathF.Sin(angle);
        // Half the screen's length along the lines and across them.
        float along = (MathF.Abs(frame.Width * ux) + MathF.Abs(frame.Height * uy)) / 2;
        float across = (MathF.Abs(frame.Width * uy) + MathF.Abs(frame.Height * ux)) / 2;
        float band = across * Math.Clamp(p.ClearBand / 100, 0, 1);
        float side = across - band;
        float longest = along * 2 * p.Length / 100;
        bool pointedBack = p.Shape is SpeedLineShape.Both or SpeedLineShape.Back;
        bool pointedFront = p.Shape is SpeedLineShape.Both or SpeedLineShape.Front;
        var center = new Point(frame.Width / 2, frame.Height / 2);
        var random = new LineRandom(p.Seed, p.Generation);
        var builder = new FigureSet.Builder();

        for (int i = 0; i < p.Count; i++)
        {
            // Each line takes its own strip across the screen, so they spread evenly, and the clear band is skipped.
            float position = (i + random.Next(i, 0)) / p.Count * side * 2;
            float offset = position < side ? -across + position : band + (position - side);

            float length = longest * (1 - p.LengthVariation / 100 * random.Next(i, 1));
            float width = p.Thickness * (1 - p.ThicknessVariation / 100 * random.Next(i, 2));
            float speed = p.Speed * (1 - p.SpeedVariation / 100 * random.Next(i, 3));
            if (length < 0.5f || width < 0.05f)
                continue;

            // The line runs off one end and comes back at the other once it is fully off the screen.
            float period = along * 2 + length;
            double travelled = random.Next(i, 4) * period + speed * p.Time;
            float middle = (float)(travelled - Math.Floor(travelled / period) * period) - along - length / 2;

            float backAlong = middle - length / 2;
            float frontAlong = middle + length / 2;
            var back = new Point(center.X + ux * backAlong - uy * offset, center.Y + uy * backAlong + ux * offset);
            var front = new Point(center.X + ux * frontAlong - uy * offset, center.Y + uy * frontAlong + ux * offset);
            LineShapes.AddStroke(builder, back, front, width / 2, pointedBack, pointedFront);
        }

        return builder.ToFigureSet();
    }
}
