using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Beutl.Extensibility;
using Beutl.Extensions.FocusLines.Graphics;
using Beutl.Extensions.FocusLines.Properties;
using Beutl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beutl.Extensions.FocusLines.Tests;

[NonParallelizable]
public class LocalizationTests
{
    private static readonly Type[] s_drawableTypes =
    [
        typeof(FocusLinesDrawable), typeof(UrchinFlashDrawable), typeof(BetaFlashDrawable),
        typeof(SpeedLinesDrawable), typeof(SunburstDrawable),
    ];

    [TestCase("ja")]
    [TestCase("zh-CN")]
    [TestCase("ko-KR")]
    [TestCase("es")]
    public void EveryTranslationIsComplete(string language)
    {
        ResourceSet neutral = Strings.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        ResourceSet? translated = Strings.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo(language), true, false);

        Assert.That(translated, Is.Not.Null, "The satellite assembly must be deployed with the extension.");
        DictionaryEntry[] entries = translated!.Cast<DictionaryEntry>().ToArray();
        Assert.That(entries.Select(entry => entry.Key), Is.EquivalentTo(neutral.Cast<DictionaryEntry>().Select(entry => entry.Key)));
        foreach (DictionaryEntry entry in entries)
            Assert.That(entry.Value, Is.TypeOf<string>().And.Not.Empty, $"{language}: {entry.Key}");
    }

    [TestCase("en-US", "Focus Lines", "Center")]
    [TestCase("ja-JP", "集中線", "中心")]
    [TestCase("zh-CN", "集中线", "中心")]
    [TestCase("ko-KR", "집중선", "중심")]
    [TestCase("es-ES", "Líneas de enfoque", "Centro")]
    [TestCase("fr-FR", "Focus Lines", "Center")]
    [TestCase("", "Focus Lines", "Center")]
    public void TheEditorUsesTheSelectedLanguage(string language, string name, string center)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            var extension = new FocusLinesExtension();
            Assert.That(extension.DisplayName, Is.EqualTo(name));
            Assert.That(extension.Name, Is.EqualTo("FocusLines"));

            Type[] types = [.. s_drawableTypes, typeof(FlashLineShape), typeof(SpeedLineShape)];
            foreach (Type type in types)
            {
                MemberInfo[] members =
                [
                    type,
                    .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                    .. type.GetFields(BindingFlags.Public | BindingFlags.Static),
                ];
                foreach (MemberInfo member in members)
                {
                    if (member.GetCustomAttribute<DisplayAttribute>() is { } display)
                    {
                        Assert.That(display.ResourceType, Is.EqualTo(typeof(Strings)), member.Name);
                        Assert.That(display.GetName(), Is.EqualTo(Strings.ResourceManager.GetString(display.Name!)), member.Name);
                        if (display.Description != null)
                            Assert.That(display.GetDescription(), Is.EqualTo(Strings.ResourceManager.GetString(display.Description)), member.Name);
                    }

                    if (member.GetCustomAttribute<CombinedEditorAttribute>() is { } combined)
                    {
                        Assert.That(combined.ResourceType, Is.EqualTo(typeof(Strings)), member.Name);
                        Assert.That(combined.GetName(), Is.EqualTo(Strings.ClearSize));
                    }
                }
            }

            using var library = new LibraryService();
            using ServiceProvider services = new ServiceCollection().AddSingleton(library).BuildServiceProvider();
            extension.Load(new ExtensionContext(services));
            GroupLibraryItem group = library.Items.OfType<GroupLibraryItem>().Single(item => item.DisplayName == name);
            LibraryItem[] items = group.Items.ToArray();
            Assert.That(items.Select(item => item.DisplayName), Is.EqualTo(s_drawableTypes.Select(type => type.GetCustomAttribute<DisplayAttribute>()!.GetName())));
            Assert.That(items.Select(item => item.Description), Is.EqualTo(new[]
            {
                Strings.FocusLinesDescription, Strings.UrchinFlashDescription, Strings.BetaFlashDescription,
                Strings.SpeedLinesDescription, Strings.SunburstDescription,
            }));

            var drawable = new FocusLinesDrawable();
            HeadlessScene.Create(drawable);
            Assert.That(drawable.GetEditingHandles(MediaTime.Zero).Single().Label, Is.EqualTo(center));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
