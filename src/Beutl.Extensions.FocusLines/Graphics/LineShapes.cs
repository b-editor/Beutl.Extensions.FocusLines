using System.ComponentModel.DataAnnotations;
using Beutl.Extensions.FocusLines.Properties;
using Beutl.Graphics;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>Which ends of a flash line come to a point.</summary>
public enum FlashLineShape
{
    [Display(Name = nameof(Strings.TaperBoth), ResourceType = typeof(Strings))]
    Both,

    [Display(Name = nameof(Strings.TaperOuter), ResourceType = typeof(Strings))]
    Outer,

    [Display(Name = nameof(Strings.TaperInner), ResourceType = typeof(Strings))]
    Inner,
}

/// <summary>Which ends of a speed line come to a point; the front is the way the line moves.</summary>
public enum SpeedLineShape
{
    [Display(Name = nameof(Strings.TaperBoth), ResourceType = typeof(Strings))]
    Both,

    [Display(Name = nameof(Strings.TaperBack), ResourceType = typeof(Strings))]
    Back,

    [Display(Name = nameof(Strings.TaperFront), ResourceType = typeof(Strings))]
    Front,

    [Display(Name = nameof(Strings.TaperNone), ResourceType = typeof(Strings))]
    None,
}

internal static class LineShapes
{
    /// <summary>
    /// Adds a straight line from <paramref name="from"/> to <paramref name="to"/>, 2 × <paramref name="half"/> wide
    /// at its widest. A pointed end comes to a point; when both ends are pointed, the line is widest at
    /// <paramref name="peak"/> of its length from <paramref name="from"/>.
    /// </summary>
    public static void AddStroke(
        FigureSet.Builder builder, Point from, Point to, float half, bool pointedFrom, bool pointedTo, float peak = 0.5f)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1e-3f || half <= 0)
            return;

        // The normal to the line, as long as the half width.
        float nx = -dy / length * half;
        float ny = dx / length * half;
        var peakPoint = new Point(from.X + dx * peak, from.Y + dy * peak);
        bool both = pointedFrom && pointedTo;

        if (pointedFrom)
            builder.Add(from);
        else
            builder.Add(from.X + nx, from.Y + ny);

        if (both)
            builder.Add(peakPoint.X + nx, peakPoint.Y + ny);

        if (pointedTo)
        {
            builder.Add(to);
        }
        else
        {
            builder.Add(to.X + nx, to.Y + ny);
            builder.Add(to.X - nx, to.Y - ny);
        }

        if (both)
            builder.Add(peakPoint.X - nx, peakPoint.Y - ny);

        if (!pointedFrom)
            builder.Add(from.X - nx, from.Y - ny);

        builder.Close();
    }
}
