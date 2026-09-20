namespace SpriteForge.Media.Normalization;

internal static class AnchorCalculator
{
    public static (int X, int Y) Calculate(string anchor, int canvasWidth, int canvasHeight, int imageWidth, int imageHeight)
    {
        var normalized = anchor.Trim().ToLowerInvariant();
        var x = normalized.Contains("left") ? 0 : normalized.Contains("right") ? canvasWidth - imageWidth : (canvasWidth - imageWidth) / 2;
        var y = normalized.Contains("top") ? 0 : normalized.Contains("bottom") ? canvasHeight - imageHeight : (canvasHeight - imageHeight) / 2;
        return (x, y);
    }
}
