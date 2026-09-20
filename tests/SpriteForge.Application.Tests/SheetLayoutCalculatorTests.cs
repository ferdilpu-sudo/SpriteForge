using SpriteForge.Application.Sheets;
using SpriteForge.Core.Models;

namespace SpriteForge.Application.Tests;

public sealed class SheetLayoutCalculatorTests
{
    [Fact]
    public void Calculate_OrdersEnabledFramesLeftToRightThenTopToBottom()
    {
        var frames = Enumerable.Range(0, 5).Select(index => Frame(index)).ToArray();
        var settings = new SheetSettings(3, 64, 32, 2, 4, false);

        var layout = new SheetLayoutCalculator().Calculate(frames, settings);

        Assert.Equal(3, layout.Columns);
        Assert.Equal(2, layout.Rows);
        Assert.Equal((2, 2), (layout.Cells[0].X, layout.Cells[0].Y));
        Assert.Equal((70, 2), (layout.Cells[1].X, layout.Cells[1].Y));
        Assert.Equal((2, 38), (layout.Cells[3].X, layout.Cells[3].Y));
    }

    [Fact]
    public void Calculate_ExcludesDisabledFrames()
    {
        var frames = new[] { Frame(0), Frame(1, enabled: false), Frame(2) };
        var layout = new SheetLayoutCalculator().Calculate(frames, new SheetSettings(2, 32, 32, 0, 0, false));

        Assert.Equal(2, layout.Cells.Count);
        Assert.DoesNotContain(layout.Cells, cell => cell.FrameId == frames[1].Id);
    }

    [Fact]
    public void Calculate_ThrowsClearly_WhenSheetExceedsSafetyLimit()
    {
        var frames = new[] { Frame(0), Frame(1) };
        var settings = new SheetSettings(2, 9_000, 64, 0, 0, false);

        var exception = Assert.Throws<InvalidOperationException>(() => new SheetLayoutCalculator().Calculate(frames, settings));

        Assert.True(exception.Message.Contains("safety limit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_ThrowsInsteadOfOverflowing_WhenDimensionsExceedIntegerRange()
    {
        var frames = new[] { Frame(0), Frame(1) };
        var settings = new SheetSettings(2, int.MaxValue, 32, int.MaxValue, int.MaxValue, false);

        var exception = Assert.Throws<InvalidOperationException>(() => new SheetLayoutCalculator().Calculate(frames, settings));

        Assert.True(exception.Message.Contains("dimensions exceed", StringComparison.OrdinalIgnoreCase));
    }

    private static FrameRecord Frame(int index, bool enabled = true) => new(
        Guid.NewGuid(), index, index, enabled, 83.333,
        new FrameArtifactLinks(null, null, null, null),
        new FrameTransform(0, 0, 1),
        new FramePivot(0.5, 1));
}
