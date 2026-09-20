namespace SpriteForge.Core.Models;

public sealed record SheetCell(
    int Index,
    Guid FrameId,
    int X,
    int Y,
    int Width,
    int Height,
    double DurationMs,
    FramePivot Pivot);

public sealed record SheetLayout(
    int Width,
    int Height,
    int Columns,
    int Rows,
    IReadOnlyList<SheetCell> Cells);
