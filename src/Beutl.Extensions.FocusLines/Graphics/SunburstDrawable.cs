using System.ComponentModel.DataAnnotations;
using Beutl.Composition;
using Beutl.Engine;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>Wedge-shaped rays that fan out from a point over the whole screen, and can turn.</summary>
/// <remarks>The centre is measured from the middle of the screen.</remarks>
[Display(Name = "放射")]
public sealed partial class SunburstDrawable : Drawable, IEditingHandleProvider
{
    public SunburstDrawable()
    {
        ScanProperties<SunburstDrawable>();
        Fill.Value = new SolidColorBrush(Color.FromArgb(96, 255, 255, 255));
    }

    [Display(Name = "中心", Description = "画面の中央からの位置。プレビューのハンドルでも動かせます")]
    public IExpressionProperty<Point> Center { get; } = Property.CreateAnimatable(default(Point));

    [Display(Name = "本数")]
    [Range(1, 360)]
    public IExpressionProperty<int> Count { get; } = Property.CreateAnimatable(16);

    [Display(Name = "線の幅", Description = "線と隙間を合わせた幅に対する、線の幅")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> RayWidth { get; } = Property.CreateAnimatable(50f);

    [Display(Name = "空白の幅", Description = "画面の大きさに対する、中心の空白の大きさ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    [CombinedEditor(nameof(ClearHeight), Name = "空白の大きさ")]
    public IExpressionProperty<float> ClearWidth { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "空白の高さ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ClearHeight { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "回転")]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Rotation { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "回転の速さ (°/秒)", Description = "正の値で時計回りに回ります")]
    [Range(-36000f, 36000f)]
    public IExpressionProperty<float> RotationSpeed { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "ねじれ", Description = "線を渦のように曲げます")]
    [Range(-720f, 720f)]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Twist { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "塗りつぶし")]
    public IExpressionProperty<Brush?> Fill { get; } = Property.CreateExpression<Brush?>();

    [Display(Name = "背景", Description = "線の間を塗ります")]
    public IExpressionProperty<Brush?> Background { get; } = Property.CreateExpression<Brush?>();

    public IReadOnlyList<EditingHandle> GetEditingHandles(MediaTime time) => CenterHandle.Get(this, Center, time);

    public new partial class Resource
    {
        private LinesGeometry.Resource? _rayShapes;
        private LinesGeometry.Resource? _clearShape;
        private SunburstParameters? _builtRays;
        private (Size, Point, float, float)? _builtClear;
        private double _time;

        partial void PostReconcile(SunburstDrawable obj, CompositionContext context)
        {
            // Rays that do not turn do not depend on the time, so their drawing is kept from frame to frame.
            double time = RotationSpeed != 0 ? (context.Time - obj.TimeRange.Value.Start).ToSeconds() : 0;
            if (time != _time)
            {
                _time = time;
                Version++;
            }
        }

        partial void PostDispose(bool disposing)
        {
            _rayShapes?.Dispose();
            _clearShape?.Dispose();
            _rayShapes = _clearShape = null;
            _builtRays = null;
            _builtClear = null;
        }

        protected override Size MeasureCore(Size availableSize) => availableSize;

        protected override void OnDraw(GraphicsContext2D context)
        {
            Size frame = context.Size;
            if (frame.Width <= 0 || frame.Height <= 0 || (Fill == null && Background == null))
                return;

            // The turn made so far is wrapped to one revolution, so long elements keep their precision.
            double turned = LineMath.Sanitize(RotationSpeed, -36000, 36000) * _time % 360;
            var rays = new SunburstParameters(
                frame,
                Center,
                Math.Clamp(Count, 0, 360),
                LineMath.Sanitize(RayWidth, 0, 100),
                (float)((LineMath.Sanitize(Rotation, float.MinValue, float.MaxValue) + turned) % 360),
                LineMath.Sanitize(Twist, -720, 720));
            var clear = (frame, Center, LineMath.Sanitize(ClearWidth, 0, 400), LineMath.Sanitize(ClearHeight, 0, 400));

            _rayShapes ??= new LinesGeometry().ToResource(CompositionContext.Default);
            _clearShape ??= new LinesGeometry().ToResource(CompositionContext.Default);
            if (_builtRays != rays)
            {
                _builtRays = rays;
                _rayShapes.SetFigures(SunburstFigures.Build(rays));
            }

            if (_builtClear != clear)
            {
                _builtClear = clear;
                var builder = new FigureSet.Builder();
                var center = new Point(frame.Width / 2 + Center.X, frame.Height / 2 + Center.Y);
                LineMath.AddEllipse(builder, center, frame.Width * clear.Item3 / 200, frame.Height * clear.Item4 / 200);
                _clearShape.SetFigures(builder.ToFigureSet());
            }

            var bounds = new Rect(frame);
            using (context.PushClip(bounds))
            {
                if (Background != null)
                    context.DrawRectangle(bounds, Background, null);

                if (Fill != null)
                {
                    using (context.PushClip(_clearShape, ClipOperation.Difference))
                    {
                        context.DrawGeometry(_rayShapes, Fill, null);
                    }
                }
            }
        }
    }
}
