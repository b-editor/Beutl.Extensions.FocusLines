using Beutl.Extensibility;
using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Extensions.FocusLines.Properties;
using Beutl.Services;

namespace Beutl.Extensions.FocusLines;

[Export]
public sealed class FocusLinesExtension : Extension
{
    public override string Name => "FocusLines";

    public override string DisplayName => Strings.FocusLines;

    public override void Load()
    {
        base.Load();
        // BindDrawable also binds the EngineObject format, which the timeline requires to accept a drop.
        LibraryService.Current.RegisterGroup(Strings.FocusLines, g => g
            .AddMultiple(Strings.FocusLines, Strings.FocusLinesDescription, m => m.BindDrawable<FocusLinesDrawable>())
            .AddMultiple(Strings.UrchinFlash, Strings.UrchinFlashDescription, m => m.BindDrawable<UrchinFlashDrawable>())
            .AddMultiple(Strings.BetaFlash, Strings.BetaFlashDescription, m => m.BindDrawable<BetaFlashDrawable>())
            .AddMultiple(Strings.SpeedLines, Strings.SpeedLinesDescription, m => m.BindDrawable<SpeedLinesDrawable>())
            .AddMultiple(Strings.Sunburst, Strings.SunburstDescription, m => m.BindDrawable<SunburstDrawable>()));
    }
}
