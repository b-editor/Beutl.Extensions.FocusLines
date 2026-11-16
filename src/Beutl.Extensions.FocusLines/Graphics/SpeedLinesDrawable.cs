using System.ComponentModel.DataAnnotations;
using Beutl.Composition;
using Beutl.Engine;
using Beutl.Extensions.FocusLines.Properties;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>Parallel lines across the whole screen that stream past in one direction.</summary>
[Display(Name = nameof(Strings.SpeedLines), ResourceType = typeof(Strings))]
public sealed partial class SpeedLinesDrawable : Drawable
{
    public SpeedLinesDrawable()
    {
        ScanProperties<SpeedLinesDrawable>();
        Fill.Value = new SolidColorBrush(Colors.White);
    }

    [Display(Name = nameof(Strings.Angle), Description = nameof(Strings.AngleDescription), ResourceType = typeof(Strings))]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Angle { get; } = Property.CreateAnimatable(0f);

    [Display(Name = nameof(Strings.Count), ResourceType = typeof(Strings))]
    [Range(1, 2000)]
    public IExpressionProperty<int> Count { get; } = Property.CreateAnimatable(90);

    [Display(Name = nameof(Strings.Length), Description = nameof(Strings.SpeedLengthDescription), ResourceType = typeof(Strings))]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> Length { get; } = Property.CreateAnimatable(45f);

    [Display(Name = nameof(Strings.LengthVariation), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> LengthVariation { get; } = Property.CreateAnimatable(60f);

    [Display(Name = nameof(Strings.Thickness), ResourceType = typeof(Strings))]
    [Range(0f, 1000f)]
    public IExpressionProperty<float> Thickness { get; } = Property.CreateAnimatable(8f);

    [Display(Name = nameof(Strings.ThicknessVariation), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ThicknessVariation { get; } = Property.CreateAnimatable(70f);

    [Display(Name = nameof(Strings.LineShape), ResourceType = typeof(Strings))]
    public IExpressionProperty<SpeedLineShape> Shape { get; } = Property.CreateAnimatable(SpeedLineShape.Both);

    [Display(Name = nameof(Strings.Speed), Description = nameof(Strings.SpeedDescription), ResourceType = typeof(Strings))]
    [Range(-100000f, 100000f)]
    public IExpressionProperty<float> Speed { get; } = Property.CreateAnimatable(3000f);

    [Display(Name = nameof(Strings.SpeedVariation), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> SpeedVariation { get; } = Property.CreateAnimatable(40f);

    [Display(Name = nameof(Strings.ClearWidth), Description = nameof(Strings.ClearBandDescription), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ClearBand { get; } = Property.CreateAnimatable(0f);

    [Display(Name = nameof(Strings.UpdateInterval), Description = nameof(Strings.UpdateIntervalSpeedLinesDescription), ResourceType = typeof(Strings))]
    [Range(0f, 60f)]
    public IExpressionProperty<float> UpdateInterval { get; } = Property.CreateAnimatable(0f);

    [Display(Name = nameof(Strings.Seed), ResourceType = typeof(Strings))]
    public IExpressionProperty<int> Seed { get; } = Property.CreateAnimatable(0);

    [Display(Name = nameof(Strings.Fill), ResourceType = typeof(Strings))]
    public IExpressionProperty<Brush?> Fill { get; } = Property.CreateExpression<Brush?>();

    [Display(Name = nameof(Strings.Stroke), ResourceType = typeof(Strings))]
    public IExpressionProperty<Pen?> Pen { get; } = Property.CreateExpression<Pen?>();

    public new partial class Resource
    {
        private LinesGeometry.Resource? _lines;
        private SpeedLinesParameters? _built;
        private long _generation;
        private double _time;

        partial void PostReconcile(SpeedLinesDrawable obj, CompositionContext context)
        {
            MediaTime local = context.Time - obj.TimeRange.Value.Start;
            long generation = LineMath.Generation(local, UpdateInterval);
            // Lines that stand still do not depend on the time, so their drawing is kept from frame to frame.
            double time = Speed != 0 ? local.ToSeconds() : 0;
            if (generation != _generation || time != _time)
            {
                _generation = generation;
                _time = time;
                Version++;
            }
        }

        partial void PostDispose(bool disposing)
        {
            _lines?.Dispose();
            _lines = null;
            _built = null;
        }

        protected override Size MeasureCore(Size availableSize) => availableSize;

        protected override void OnDraw(GraphicsContext2D context)
        {
            Size frame = context.Size;
            if (frame.Width <= 0 || frame.Height <= 0 || (Fill == null && Pen == null))
                return;

            var parameters = new SpeedLinesParameters(
                frame,
                LineMath.Sanitize(Angle, float.MinValue, float.MaxValue),
                Math.Clamp(Count, 0, 2000),
                LineMath.Sanitize(Length, 0, 400),
                LineMath.Sanitize(LengthVariation, 0, 100),
                LineMath.Sanitize(Thickness, 0, 1000),
                LineMath.Sanitize(ThicknessVariation, 0, 100),
                Shape,
                LineMath.Sanitize(Speed, -100000, 100000),
                LineMath.Sanitize(SpeedVariation, 0, 100),
                LineMath.Sanitize(ClearBand, 0, 100),
                Seed,
                _generation,
                _time);

            _lines ??= new LinesGeometry().ToResource(CompositionContext.Default);
            if (_built != parameters)
            {
                _built = parameters;
                _lines.SetFigures(SpeedLinesFigures.Build(parameters));
            }

            using (context.PushClip(new Rect(frame)))
            {
                context.DrawGeometry(_lines, Fill, Pen);
            }
        }
    }
}
