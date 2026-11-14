using System.ComponentModel.DataAnnotations;
using Beutl.Composition;
using Beutl.Engine;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>Parallel lines across the whole screen that stream past in one direction.</summary>
[Display(Name = "流線")]
public sealed partial class SpeedLinesDrawable : Drawable
{
    public SpeedLinesDrawable()
    {
        ScanProperties<SpeedLinesDrawable>();
        Fill.Value = new SolidColorBrush(Colors.White);
    }

    [Display(Name = "角度", Description = "線の向き。0で右へ流れます")]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Angle { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "本数")]
    [Range(1, 2000)]
    public IExpressionProperty<int> Count { get; } = Property.CreateAnimatable(90);

    [Display(Name = "長さ", Description = "線の向きの画面の長さに対する、線の長さ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> Length { get; } = Property.CreateAnimatable(45f);

    [Display(Name = "長さのばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> LengthVariation { get; } = Property.CreateAnimatable(60f);

    [Display(Name = "太さ")]
    [Range(0f, 1000f)]
    public IExpressionProperty<float> Thickness { get; } = Property.CreateAnimatable(8f);

    [Display(Name = "太さのばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ThicknessVariation { get; } = Property.CreateAnimatable(70f);

    [Display(Name = "線の形")]
    public IExpressionProperty<SpeedLineShape> Shape { get; } = Property.CreateAnimatable(SpeedLineShape.Both);

    [Display(Name = "速さ (px/秒)", Description = "線が流れる速さ。0で止まります")]
    [Range(-100000f, 100000f)]
    public IExpressionProperty<float> Speed { get; } = Property.CreateAnimatable(3000f);

    [Display(Name = "速さのばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> SpeedVariation { get; } = Property.CreateAnimatable(40f);

    [Display(Name = "空白の幅", Description = "中央に線を描かない帯の幅。画面に対する割合")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ClearBand { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "更新間隔 (秒)", Description = "この間隔で線を描き直します。0で描き直しません")]
    [Range(0f, 60f)]
    public IExpressionProperty<float> UpdateInterval { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "シード")]
    public IExpressionProperty<int> Seed { get; } = Property.CreateAnimatable(0);

    [Display(Name = "塗りつぶし")]
    public IExpressionProperty<Brush?> Fill { get; } = Property.CreateExpression<Brush?>();

    [Display(Name = "ストローク")]
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
