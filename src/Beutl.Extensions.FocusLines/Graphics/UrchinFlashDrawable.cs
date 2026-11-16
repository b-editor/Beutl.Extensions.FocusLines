using System.ComponentModel.DataAnnotations;
using Beutl.Composition;
using Beutl.Engine;
using Beutl.Extensions.FocusLines.Properties;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>
/// Spines that bristle out from round a clear middle, like a sea urchin. It is placed and sized like a shape:
/// its bounds are the farthest its lines can reach.
/// </summary>
[Display(Name = nameof(Strings.UrchinFlash), ResourceType = typeof(Strings))]
public sealed partial class UrchinFlashDrawable : Drawable
{
    public UrchinFlashDrawable()
    {
        ScanProperties<UrchinFlashDrawable>();
        // White lines outlined in black, which show on any background.
        Fill.Value = new SolidColorBrush(Colors.White);
        var pen = new Media.Pen();
        pen.Brush.Value = new SolidColorBrush(Colors.Black);
        pen.Thickness.Value = 3;
        pen.StrokeJoin.Value = StrokeJoin.Round;
        pen.StrokeOrder.Value = StrokeOrder.BelowFill;
        Pen.Value = pen;
    }

    [Display(Name = nameof(Strings.Count), ResourceType = typeof(Strings))]
    [Range(1, 3000)]
    public IExpressionProperty<int> Count { get; } = Property.CreateAnimatable(260);

    [Display(Name = nameof(Strings.ClearWidth), Description = nameof(Strings.ClearSizePixelsDescription), ResourceType = typeof(Strings))]
    [Range(0f, 10000f)]
    [CombinedEditor(nameof(ClearHeight), Name = nameof(Strings.ClearSize), ResourceType = typeof(Strings))]
    public IExpressionProperty<float> ClearWidth { get; } = Property.CreateAnimatable(360f);

    [Display(Name = nameof(Strings.ClearHeight), ResourceType = typeof(Strings))]
    [Range(0f, 10000f)]
    public IExpressionProperty<float> ClearHeight { get; } = Property.CreateAnimatable(360f);

    [Display(Name = nameof(Strings.Length), ResourceType = typeof(Strings))]
    [Range(0f, 10000f)]
    public IExpressionProperty<float> Length { get; } = Property.CreateAnimatable(260f);

    [Display(Name = nameof(Strings.LengthVariation), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> LengthVariation { get; } = Property.CreateAnimatable(60f);

    [Display(Name = nameof(Strings.Thickness), ResourceType = typeof(Strings))]
    [Range(0f, 1000f)]
    public IExpressionProperty<float> Thickness { get; } = Property.CreateAnimatable(7f);

    [Display(Name = nameof(Strings.ThicknessVariation), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ThicknessVariation { get; } = Property.CreateAnimatable(50f);

    [Display(Name = nameof(Strings.SpacingVariation), ResourceType = typeof(Strings))]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> SpacingVariation { get; } = Property.CreateAnimatable(100f);

    [Display(Name = nameof(Strings.Jaggedness), Description = nameof(Strings.JaggednessDescription), ResourceType = typeof(Strings))]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> Jaggedness { get; } = Property.CreateAnimatable(10f);

    [Display(Name = nameof(Strings.LineShape), ResourceType = typeof(Strings))]
    public IExpressionProperty<FlashLineShape> Shape { get; } = Property.CreateAnimatable(FlashLineShape.Both);

    [Display(Name = nameof(Strings.Rotation), ResourceType = typeof(Strings))]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Rotation { get; } = Property.CreateAnimatable(0f);

    [Display(Name = nameof(Strings.UpdateInterval), Description = nameof(Strings.UpdateIntervalLinesDescription), ResourceType = typeof(Strings))]
    [Range(0f, 60f)]
    public IExpressionProperty<float> UpdateInterval { get; } = Property.CreateAnimatable(0.1f);

    [Display(Name = nameof(Strings.Seed), ResourceType = typeof(Strings))]
    public IExpressionProperty<int> Seed { get; } = Property.CreateAnimatable(0);

    [Display(Name = nameof(Strings.Fill), ResourceType = typeof(Strings))]
    public IExpressionProperty<Brush?> Fill { get; } = Property.CreateExpression<Brush?>();

    [Display(Name = nameof(Strings.Stroke), ResourceType = typeof(Strings))]
    public IExpressionProperty<Pen?> Pen { get; } = Property.CreateExpression<Pen?>();

    public new partial class Resource
    {
        private LinesGeometry.Resource? _lines;
        private (UrchinFlashParameters, Point)? _built;
        private long _generation;

        partial void PostReconcile(UrchinFlashDrawable obj, CompositionContext context)
        {
            long generation = LineMath.Generation(context.Time - obj.TimeRange.Value.Start, UpdateInterval);
            if (generation != _generation)
            {
                _generation = generation;
                Version++;
            }
        }

        partial void PostDispose(bool disposing)
        {
            _lines?.Dispose();
            _lines = null;
            _built = null;
        }

        private UrchinFlashParameters GetParameters() => new(
            Math.Clamp(Count, 0, 3000),
            LineMath.Sanitize(ClearWidth, 0, 10000),
            LineMath.Sanitize(ClearHeight, 0, 10000),
            LineMath.Sanitize(Length, 0, 10000),
            LineMath.Sanitize(LengthVariation, 0, 100),
            LineMath.Sanitize(Thickness, 0, 1000),
            LineMath.Sanitize(ThicknessVariation, 0, 100),
            LineMath.Sanitize(SpacingVariation, 0, 100),
            LineMath.Sanitize(Jaggedness, 0, 400),
            Shape,
            LineMath.Sanitize(Rotation, float.MinValue, float.MaxValue),
            Seed,
            _generation);

        // Room round the lines for a stroke on their outside.
        private float StrokeMargin => Pen is { } pen ? MathF.Max(0, pen.Thickness) : 0;

        protected override Size MeasureCore(Size availableSize)
        {
            Size extent = UrchinFlashFigures.GetExtent(GetParameters());
            float margin = StrokeMargin;
            return new Size((extent.Width + margin) * 2, (extent.Height + margin) * 2);
        }

        protected override void OnDraw(GraphicsContext2D context)
        {
            if (Fill == null && Pen == null)
                return;

            UrchinFlashParameters parameters = GetParameters();
            Size extent = UrchinFlashFigures.GetExtent(parameters);
            float margin = StrokeMargin;
            var center = new Point(extent.Width + margin, extent.Height + margin);

            _lines ??= new LinesGeometry().ToResource(CompositionContext.Default);
            if (_built != (parameters, center))
            {
                _built = (parameters, center);
                _lines.SetFigures(UrchinFlashFigures.Build(parameters, center));
            }

            context.DrawGeometry(_lines, Fill, Pen);
        }
    }
}
