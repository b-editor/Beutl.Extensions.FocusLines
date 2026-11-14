using System.Text.Json.Nodes;
using Beutl.Animation;
using Beutl.Engine;
using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Graphics;
using Beutl.Media;
using Beutl.ProjectSystem;
using Beutl.Serialization;
using Beutl.Services;

namespace Beutl.Extensions.FocusLines.Tests;

public class EditingTests
{
    [Test]
    public void TheCenterHandleSitsOnTheCenterAndEditsIt()
    {
        var drawable = new FocusLinesDrawable();
        drawable.Center.Value = new Point(100, -50);
        HeadlessScene.Create(drawable);

        EditingHandle handle = drawable.GetEditingHandles(MediaTime.Zero).Single();

        Assert.That(handle.Position, Is.EqualTo(new Point(1060, 490)));
        Assert.That(handle.Property, Is.SameAs(drawable.Center));
    }

    [Test]
    public void TheCenterHandleFollowsAnAnimatedCenter()
    {
        var drawable = new BetaFlashDrawable();
        var animation = new KeyFrameAnimation<Point>();
        animation.KeyFrames.Add(new KeyFrame<Point> { KeyTime = { Value = MediaTime.Zero }, Value = { Value = new Point(0, 0) } });
        animation.KeyFrames.Add(new KeyFrame<Point> { KeyTime = { Value = MediaTime.FromSeconds(2) }, Value = { Value = new Point(200, 0) } });
        drawable.Center.SetKeyFrameAnimation(animation);
        HeadlessScene.Create(drawable);

        EditingHandle handle = drawable.GetEditingHandles(MediaTime.FromSeconds(1)).Single();

        Assert.That(handle.Position.X, Is.EqualTo(1060).Within(0.01));
        Assert.That(handle.Position.Y, Is.EqualTo(540).Within(0.01));
    }

    [Test]
    public void ADrawableOutsideAScenehasNoHandle()
    {
        Assert.That(new SunburstDrawable().GetEditingHandles(MediaTime.Zero), Is.Empty);
    }

    [Test]
    public void EveryKindCanBeDroppedOnTheTimeline()
    {
        new FocusLinesExtension().Load();

        Type[] types =
        [
            typeof(FocusLinesDrawable), typeof(UrchinFlashDrawable), typeof(BetaFlashDrawable),
            typeof(SpeedLinesDrawable), typeof(SunburstDrawable),
        ];
        // The timeline only accepts library items that carry the EngineObject format; the Drawable format alone
        // shows in the library but is refused on drop.
        Assert.That(LibraryService.Current.GetTypesFromFormat(KnownLibraryItemFormats.EngineObject), Is.SupersetOf(types));
        Assert.That(LibraryService.Current.GetTypesFromFormat(KnownLibraryItemFormats.Drawable), Is.SupersetOf(types));
    }

    [Test]
    public void ValuesSurviveSaving()
    {
        var focus = new FocusLinesDrawable();
        focus.Center.Value = new Point(-120, 45);
        focus.Count.Value = 200;
        focus.Twist.Value = 30;
        var urchin = new UrchinFlashDrawable();
        urchin.Shape.Value = FlashLineShape.Outer;
        var beta = new BetaFlashDrawable();
        beta.Flash.Value = null;
        var speed = new SpeedLinesDrawable();
        speed.Shape.Value = SpeedLineShape.Front;
        speed.Angle.Value = 15;
        var sunburst = new SunburstDrawable();
        sunburst.RotationSpeed.Value = 12;

        var element = new Element { Length = { Value = MediaTime.FromSeconds(5) } };
        foreach (Drawable drawable in new Drawable[] { focus, urchin, beta, speed, sunburst })
            element.AddObject(drawable);

        JsonObject json = CoreSerializer.SerializeToJsonObject(element);
        var restored = (Element)CoreSerializer.DeserializeFromJsonObject(json, typeof(Element));
        Drawable[] objects = restored.Objects.OfType<Drawable>().ToArray();

        var restoredFocus = (FocusLinesDrawable)objects[0];
        Assert.That(restoredFocus.Center.Value, Is.EqualTo(new Point(-120, 45)));
        Assert.That(restoredFocus.Count.Value, Is.EqualTo(200));
        Assert.That(restoredFocus.Twist.Value, Is.EqualTo(30));
        Assert.That(restoredFocus.Fill.Value, Is.InstanceOf<SolidColorBrush>());

        var restoredUrchin = (UrchinFlashDrawable)objects[1];
        Assert.That(restoredUrchin.Shape.Value, Is.EqualTo(FlashLineShape.Outer));
        Assert.That(restoredUrchin.Pen.Value?.StrokeOrder.Value, Is.EqualTo(StrokeOrder.BelowFill));

        Assert.That(((BetaFlashDrawable)objects[2]).Flash.Value, Is.Null);
        Assert.That(((SpeedLinesDrawable)objects[3]).Shape.Value, Is.EqualTo(SpeedLineShape.Front));
        Assert.That(((SpeedLinesDrawable)objects[3]).Angle.Value, Is.EqualTo(15));
        Assert.That(((SunburstDrawable)objects[4]).RotationSpeed.Value, Is.EqualTo(12));
    }
}
