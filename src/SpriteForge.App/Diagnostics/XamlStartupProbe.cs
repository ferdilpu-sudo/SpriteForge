using SpriteForge.App.Controls;
using SpriteForge.App.Views.Stages;

namespace SpriteForge.App.Diagnostics;

internal static class XamlStartupProbe
{
    public static void Run()
    {
        Probe("FileImageControl", static () => new FileImageControl());
        Probe("CheckerboardControl", static () => new CheckerboardControl());
        Probe("FrameStripControl", static () => new FrameStripControl());
        Probe("SourceStageView", static () => new SourceStageView());
        Probe("AnimateStageView", static () => new AnimateStageView());
        Probe("FramesStageView", static () => new FramesStageView());
        Probe("CutoutStageView", static () => new CutoutStageView());
        Probe("AlignStageView", static () => new AlignStageView());
        Probe("LoopStageView", static () => new LoopStageView());
        Probe("SheetStageView", static () => new SheetStageView());
        Probe("ExportStageView", static () => new ExportStageView());
    }

    private static void Probe(string name, Func<object> factory)
    {
        try
        {
            _ = factory();
            StartupLog.Write($"XAML probe PASS: {name}");
        }
        catch (Exception ex)
        {
            StartupLog.Write($"XAML probe FAIL: {name}", ex);
        }
    }
}
