using Beutl.Extensibility;
using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Services;

namespace Beutl.Extensions.FocusLines;

[Export]
public sealed class FocusLinesExtension : Extension
{
    public override string Name => "FocusLines";

    public override string DisplayName => "集中線";

    public override void Load()
    {
        base.Load();
        // BindDrawable also binds the EngineObject format, which the timeline requires to accept a drop.
        LibraryService.Current.RegisterGroup("集中線", g => g
            .AddMultiple("集中線", "中心に向かって集まる線で、画面を覆います", m => m.BindDrawable<FocusLinesDrawable>())
            .AddMultiple("ウニフラッシュ", "中心の周りから外へとげのように広がる線です", m => m.BindDrawable<UrchinFlashDrawable>())
            .AddMultiple("ベタフラッシュ", "画面を塗りつぶし、中心から光が広がります", m => m.BindDrawable<BetaFlashDrawable>())
            .AddMultiple("流線", "一方向へ流れる平行な線で、画面を覆います", m => m.BindDrawable<SpeedLinesDrawable>())
            .AddMultiple("放射", "中心から扇形に広がる線で、画面を覆います", m => m.BindDrawable<SunburstDrawable>()));
    }
}
