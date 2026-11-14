using System.ComponentModel.DataAnnotations;
using Beutl.Composition;
using Beutl.Engine;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>
/// A solid fill over the whole screen with a clear area round a point, from which rays of light burst into the fill.
/// </summary>
/// <remarks>The clear area shows what is below; its centre is measured from the middle of the screen.</remarks>
[Display(Name = "ベタフラッシュ")]
public sealed partial class BetaFlashDrawable : Drawable, IEditingHandleProvider
{
    public BetaFlashDrawable()
    {
        ScanProperties<BetaFlashDrawable>();
        Fill.Value = new SolidColorBrush(Colors.Black);
        Flash.Value = new SolidColorBrush(Colors.White);
    }

    [Display(Name = "中心", Description = "画面の中央からの位置。プレビューのハンドルでも動かせます")]
    public IExpressionProperty<Point> Center { get; } = Property.CreateAnimatable(default(Point));

    [Display(Name = "本数")]
    [Range(1, 2000)]
    public IExpressionProperty<int> Count { get; } = Property.CreateAnimatable(90);

    [Display(Name = "空白の幅", Description = "画面の大きさに対する、中心の空白の大きさ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    [CombinedEditor(nameof(ClearHeight), Name = "空白の大きさ")]
    public IExpressionProperty<float> ClearWidth { get; } = Property.CreateAnimatable(36f);

    [Display(Name = "空白の高さ")]
    [Range(0f, 400f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ClearHeight { get; } = Property.CreateAnimatable(36f);

    [Display(Name = "長さ", Description = "空白の端から画面の端までに対する、光の長さ")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> Length { get; } = Property.CreateAnimatable(75f);

    [Display(Name = "長さのばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> LengthVariation { get; } = Property.CreateAnimatable(70f);

    [Display(Name = "太さ", Description = "根元での光の太さ")]
    [Range(0f, 1000f)]
    public IExpressionProperty<float> Thickness { get; } = Property.CreateAnimatable(30f);

    [Display(Name = "太さのばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> ThicknessVariation { get; } = Property.CreateAnimatable(50f);

    [Display(Name = "間隔のばらつき")]
    [Range(0f, 100f)]
    [Unit(PropertyUnit.Percent)]
    public IExpressionProperty<float> SpacingVariation { get; } = Property.CreateAnimatable(100f);

    [Display(Name = "回転")]
    [Unit(PropertyUnit.Degrees)]
    public IExpressionProperty<float> Rotation { get; } = Property.CreateAnimatable(0f);

    [Display(Name = "更新間隔 (秒)", Description = "この間隔で光を描き直します。0で止まります")]
    [Range(0f, 60f)]
    public IExpressionProperty<float> UpdateInterval { get; } = Property.CreateAnimatable(0.1f);

    [Display(Name = "シード")]
    public IExpressionProperty<int> Seed { get; } = Property.CreateAnimatable(0);

    [Display(Name = "塗りつぶし", Description = "光と空白の外側")]
    public IExpressionProperty<Brush?> Fill { get; } = Property.CreateExpression<Brush?>();

    [Display(Name = "光", Description = "なしにすると、光の形に抜けます")]
    public IExpressionProperty<Brush?> Flash { get; } = Property.CreateExpression<Brush?>();

    public IReadOnlyList<EditingHandle> GetEditingHandles(MediaTime time) => CenterHandle.Get(this, Center, time);

    public new partial class Resource
    {
        private LinesGeometry.Resource? _clearShape;
        private LinesGeometry.Resource? _rayShapes;
        private LinesGeometry.Resource? _flashShape;
        private BetaFlashParameters? _built;
        private long _generation;

        partial void PostReconcile(BetaFlashDrawable obj, CompositionContext context)
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
            _clearShape?.Dispose();
            _rayShapes?.Dispose();
            _flashShape?.Dispose();
            _clearShape = _rayShapes = _flashShape = null;
            _built = null;
        }

        protected override Size MeasureCore(Size availableSize) => availableSize;

        protected override void OnDraw(GraphicsContext2D context)
        {
            Size frame = context.Size;
            if (frame.Width <= 0 || frame.Height <= 0 || (Fill == null && Flash == null))
                return;

            var parameters = new BetaFlashParameters(
                frame,
                Center,
                Math.Clamp(Count, 0, 2000),
                LineMath.Sanitize(ClearWidth, 0, 400),
                LineMath.Sanitize(ClearHeight, 0, 400),
                LineMath.Sanitize(Length, 0, 100),
                LineMath.Sanitize(LengthVariation, 0, 100),
                LineMath.Sanitize(Thickness, 0, 1000),
                LineMath.Sanitize(ThicknessVariation, 0, 100),
                LineMath.Sanitize(SpacingVariation, 0, 100),
                LineMath.Sanitize(Rotation, float.MinValue, float.MaxValue),
                Seed,
                _generation);

            _clearShape ??= new LinesGeometry().ToResource(CompositionContext.Default);
            _rayShapes ??= new LinesGeometry().ToResource(CompositionContext.Default);
            _flashShape ??= new LinesGeometry().ToResource(CompositionContext.Default);
            if (_built != parameters)
            {
                _built = parameters;
                BetaFlashFigureSets figures = BetaFlashFigures.Build(parameters);
                _clearShape.SetFigures(figures.Clear);
                _rayShapes.SetFigures(figures.Rays);
                _flashShape.SetFigures(figures.Flash);
            }

            var bounds = new Rect(frame);
            using (context.PushClip(bounds))
            {
                if (Flash != null)
                {
                    // The rays are drawn over the fill, so no seam shows along their edges.
                    using (context.PushClip(_clearShape, ClipOperation.Difference))
                    {
                        if (Fill != null)
                            context.DrawRectangle(bounds, Fill, null);

                        context.DrawGeometry(_rayShapes, Flash, null);
                    }
                }
                else
                {
                    using (context.PushClip(_flashShape, ClipOperation.Difference))
                    {
                        context.DrawRectangle(bounds, Fill!, null);
                    }
                }
            }
        }
    }
}
