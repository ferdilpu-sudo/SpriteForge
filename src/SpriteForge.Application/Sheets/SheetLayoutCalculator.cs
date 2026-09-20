using SpriteForge.Core.Models;

namespace SpriteForge.Application.Sheets;

public sealed class SheetLayoutCalculator
{
    public const int MaxSheetDimension = 16_384;
    public SheetLayout Calculate(IReadOnlyList<FrameRecord> frames, SheetSettings settings)
    {
        var enabled = frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0)
        {
            throw new ArgumentException("At least one enabled frame is required.", nameof(frames));
        }

        Validate(settings);
        var columns = settings.Columns ?? (int)Math.Ceiling(Math.Sqrt(enabled.Length));
        columns = Math.Clamp(columns, 1, enabled.Length);
        var rows = (int)Math.Ceiling(enabled.Length / (double)columns);

        var contentWidth = CheckedDimension(settings.Padding, columns, settings.CellWidth, settings.Spacing);
        var contentHeight = CheckedDimension(settings.Padding, rows, settings.CellHeight, settings.Spacing);
        EnsureWithinSafetyLimit(contentWidth, contentHeight);
        var width = settings.PowerOfTwo ? NextPowerOfTwo(contentWidth) : contentWidth;
        var height = settings.PowerOfTwo ? NextPowerOfTwo(contentHeight) : contentHeight;
        EnsureWithinSafetyLimit(width, height);

        var cells = enabled.Select((frame, index) =>
        {
            var column = index % columns;
            var row = index / columns;
            var x = settings.Padding + column * (settings.CellWidth + settings.Spacing);
            var y = settings.Padding + row * (settings.CellHeight + settings.Spacing);
            return new SheetCell(index, frame.Id, x, y, settings.CellWidth, settings.CellHeight, frame.DurationMs, frame.Pivot);
        }).ToArray();

        return new SheetLayout(width, height, columns, rows, cells);
    }

    private static void Validate(SheetSettings settings)
    {
        if (settings.CellWidth <= 0 || settings.CellHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Cell dimensions must be positive.");
        }

        if (settings.Padding < 0 || settings.Spacing < 0 || settings.Columns is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Columns, padding, and spacing are invalid.");
        }
    }

    private static void EnsureWithinSafetyLimit(int width, int height)
    {
        if (width > MaxSheetDimension || height > MaxSheetDimension)
            throw new InvalidOperationException($"Sprite sheet {width}×{height} exceeds the V1 safety limit of {MaxSheetDimension}px per side.");
    }

    private static int CheckedDimension(int padding, int count, int cellSize, int spacing)
    {
        var value = checked((long)padding * 2 + (long)count * cellSize + (long)Math.Max(0, count - 1) * spacing);
        if (value > int.MaxValue)
            throw new InvalidOperationException("Sprite sheet dimensions exceed the supported integer range.");
        return (int)value;
    }

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 1) return 1;
        var power = 1;
        while (power < value)
        {
            checked { power <<= 1; }
        }
        return power;
    }
}
