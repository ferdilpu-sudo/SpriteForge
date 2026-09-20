using SkiaSharp;

namespace SpriteForge.Media.Normalization;

internal static class AlphaBounds
{
    public static SKRectI Find(SKBitmap image)
    {
        var minX = image.Width;
        var minY = image.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                if (image.GetPixel(x, y).Alpha == 0) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX || maxY < minY
            ? SKRectI.Create(0, 0, image.Width, image.Height)
            : new SKRectI(minX, minY, maxX + 1, maxY + 1);
    }
}
