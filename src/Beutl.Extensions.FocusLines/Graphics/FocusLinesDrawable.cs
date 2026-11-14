using System.ComponentModel.DataAnnotations;
using Beutl.Composition;
using Beutl.Engine;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>Lines that converge on a point from beyond the edges of the screen, leaving a clear area round it.</summary>
/// <remarks>The drawable covers the whole screen; its centre is measured from the middle of the screen.</remarks>
[Display(Name = "集中線")]
public sealed partial class FocusLinesDrawable : Drawable, IEditingHandleProvider
{
    public FocusLinesDrawable()
    {
        ScanProperties<FocusLinesDrawable>();
        Fill.Value = new SolidColorBrush(Colors.White);
    }

    [Display(Name = "中心", Description = "画面の中央からの位置。プレビューのハンドルでも動かせます")]
    public IExpressionProperty<Point> Center { get; } = Property.CreateAnimatable(default(Point));

    [Display(Name = "本数")]
    [Range(1, 2000)]
    public IExpressionProperty<int> Count { get; } = Property.CreateAnimatable(120);

    [Display(Name = "太さ", Description = "画面の端での線の太さ")]
    [Range(0f, 1000f)]
    public IExpressionProperty<float> Thickness { get; } = Property.CreateAnimatable(16f);

    [Display(Name = "太さのばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ThicknessVariation { get; } = Property.CreateAnimatable(70f);

    [Display(Name = "間隔のばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> SpacingVariation { get; } = Property.CreateAnimatable(100f);

    [Display(Name = "空白の幅", Description = "画面の大きさに対する、中心の空白の大きさ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    [CombinedEditor(nameof(ClearHeight), Name = "空白の大きさ")]
    public IExpressionProperty<float> ClearWidth { get; } = Property.CreateAnimatable(50f);

    [Display(Name = "空白の高さ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ClearHeight { get; } = Property.CreateAnimatable(50f);

    [Display(Name = "ギザギザ", Description = "線の始まる位置のばらつき。空白の大きさに対する割合")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> Jaggedness { get; } = Property.CreateAnimatable(40f);

    [Display(Name = "回転")]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Rotation { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "ねじれ", Description = "線を渦のように曲げます")]
    [Range(-720f, 720f)]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Twist { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "更新間隔 (秒)", Description = "この間隔で線を描き直します。0で止まります")]
    [Range(0f, 60f)]
    public IExpressionProperty<float> UpdateInterval { get; } = Property.CreateAnimatable(0.1f);

    [Display(Name = "シード")]
    public IExpressionProperty<int> Seed { get; } = Property.CreateAnimatable(0);

    [Display(Name = "塗りつぶし")]
    public IExpressionProperty<Brush?> Fill { get; } = Property.CreateExpression<Brush?>();

    [Display(Name = "ストローク")]
    public IExpressionProperty<Pen?> Pen { get; } = Property.CreateExpression<Pen?>();

    public IReadOnlyList<EditingHandle> GetEditingHandles(MediaTime time) => CenterHandle.Get(this, Center, time);

    public new partial class Resource
    {
        private LinesGeometry.Resource? _lines;
        private FocusLinesParameters? _built;
        private long _generation;

        partial void PostReconcile(FocusLinesDrawable obj, CompositionContext context)
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

        protected override Size MeasureCore(Size availableSize) => availableSize;

        protected override void OnDraw(GraphicsContext2D context)
        {
            Size frame = context.Size;
            if (frame.Width <= 0 || frame.Height <= 0 || (Fill == null && Pen == null))
                return;

            LinesGeometry.Resource lines = Update(frame);
            using (context.PushClip(new Rect(frame)))
            {
                context.DrawGeometry(lines, Fill, Pen);
            }
        }

        private LinesGeometry.Resource Update(Size frame)
        {
            var parameters = new FocusLinesParameters(
                frame,
                Center,
                Math.Clamp(Count, 0, 2000),
                LineMath.Sanitize(Thickness, 0, 1000),
                LineMath.Sanitize(ThicknessVariation, 0, 100),
                LineMath.Sanitize(SpacingVariation, 0, 100),
                LineMath.Sanitize(ClearWidth, 0, 400),
                LineMath.Sanitize(ClearHeight, 0, 400),
                LineMath.Sanitize(Jaggedness, 0, 400),
                LineMath.Sanitize(Rotation, float.MinValue, float.MaxValue),
                LineMath.Sanitize(Twist, -720, 720),
                Seed,
                _generation);

            _lines ??= new LinesGeometry().ToResource(CompositionContext.Default);
            if (_built != parameters)
            {
                _built = parameters;
                _lines.SetFigures(FocusLinesFigures.Build(parameters));
            }

            return _lines;
        }
    }
}
